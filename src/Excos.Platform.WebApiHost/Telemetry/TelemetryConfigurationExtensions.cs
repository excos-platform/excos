// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Excos.Platform.WebApiHost.Telemetry;

public static class TelemetryConfigurationExtensions
{
	public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
	{
		builder.Logging.AddOpenTelemetry(logging =>
		{
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		builder.Services.AddOpenTelemetry()
			.WithMetrics(metrics =>
			{
				metrics.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddRuntimeInstrumentation();
			})
			.WithTracing(tracing =>
			{
				tracing.AddAspNetCoreInstrumentation()
					// Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
					//.AddGrpcClientInstrumentation()
					.AddHttpClientInstrumentation();
			});

		builder.AddOpenTelemetryExporters();

		return builder;
	}

	private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
	{
		bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

		if (useOtlpExporter)
		{
			builder.Services.AddOpenTelemetry().UseOtlpExporter();
		}

		// Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
		//if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
		//{
		//    builder.Services.AddOpenTelemetry()
		//       .UseAzureMonitor();
		//}

		return builder;
	}
}