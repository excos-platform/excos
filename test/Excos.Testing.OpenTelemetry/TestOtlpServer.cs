// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Collections.Concurrent;
using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Excos.Testing.OpenTelemetry;

public class TestOtlpServer
{
	private readonly TraceCaptureService traceCaptureService;
	private readonly LogsCaptureService logsCaptureService;
	private long updateCounter = 0;
	private readonly WebApplication grpcServer;

	public int Port => new Uri(this.grpcServer.Services.GetService<IServer>()!.Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;

	public TestOtlpServer()
	{
		this.traceCaptureService = new(() => Interlocked.Increment(ref this.updateCounter));
		this.logsCaptureService = new(() => Interlocked.Increment(ref this.updateCounter));

		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.WebHost.ConfigureKestrel(options =>
		{
			options.Listen(IPAddress.Loopback, 0, listenOptions =>
			{
				listenOptions.Protocols = HttpProtocols.Http2;
				listenOptions.UseHttps();
			});
		});

		builder.Services.AddGrpc();
		builder.Services.AddSingleton(this.traceCaptureService);
		builder.Services.AddSingleton(this.logsCaptureService);

		this.grpcServer = builder.Build();

		this.grpcServer.MapGrpcService<TraceCaptureService>();
		this.grpcServer.MapGrpcService<LogsCaptureService>();

		Task.Run(() => this.grpcServer.StartAsync());
	}

	public IReadOnlyList<RecordedSpan> GetSpans() => this.traceCaptureService.Spans.ToList();

	public IReadOnlyList<RecordedLog> GetLogs() => this.logsCaptureService.Logs.ToList();

	public void Clear()
	{
		while (this.traceCaptureService.Spans.TryTake(out _)) { }
		while (this.logsCaptureService.Logs.TryTake(out _)) { }
	}

	public void Shutdown()
	{
		this.grpcServer.StopAsync().Wait();
	}

	public async Task WaitForEvents()
	{
		// We will wait for up to 600ms for any trace or logs to come in
		long currentCounterValue = Interlocked.Read(ref this.updateCounter);
		int tries = 5;
		do
		{
			await Task.Delay(200); 
			tries--;
		}
		while (Interlocked.Read(ref this.updateCounter) == currentCounterValue && tries > 0);
	}

	private class TraceCaptureService(Action onUpdate) : TraceService.TraceServiceBase
	{
		public ConcurrentBag<RecordedSpan> Spans { get; } = new();
		public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
		{
			onUpdate();
			foreach (global::OpenTelemetry.Proto.Trace.V1.ResourceSpans? resourceSpans in request.ResourceSpans)
			{
				foreach (global::OpenTelemetry.Proto.Trace.V1.ScopeSpans? scopeSpans in resourceSpans.ScopeSpans)
				{
					foreach (global::OpenTelemetry.Proto.Trace.V1.Span? span in scopeSpans.Spans)
					{
						this.Spans.Add(new RecordedSpan(span));
					}
				}
			}

			return Task.FromResult(new ExportTraceServiceResponse());
		}
	}

	private class LogsCaptureService(Action onUpdate) : LogsService.LogsServiceBase
	{
		public ConcurrentBag<RecordedLog> Logs { get; } = new();
		public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
		{
			onUpdate();
			foreach (global::OpenTelemetry.Proto.Logs.V1.ResourceLogs? resourceLogs in request.ResourceLogs)
			{
				foreach (global::OpenTelemetry.Proto.Logs.V1.ScopeLogs? scopeLogs in resourceLogs.ScopeLogs)
				{
					foreach (global::OpenTelemetry.Proto.Logs.V1.LogRecord? logRecord in scopeLogs.LogRecords)
					{
						this.Logs.Add(new RecordedLog(logRecord));
					}
				}
			}
			return Task.FromResult(new ExportLogsServiceResponse());
		}
	}

	private class ExceptionInterceptor : Interceptor
	{
		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
			TRequest request,
			ServerCallContext context,
			UnaryServerMethod<TRequest, TResponse> continuation)
		{
			try
			{
				return await continuation(request, context);
			}
			catch (IOException)
			{
				throw new RpcException(new Status(StatusCode.Cancelled, "Client disconnected"));
			}
		}
	}
}