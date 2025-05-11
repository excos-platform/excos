// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Aspire.Hosting;
using Excos.Testing.OpenTelemetry;

namespace Excos.Platform.AppHostTests;

public static class AppHost
{
	private static readonly Lock OtlpServerLock = new Lock();
	public static TestOtlpServer TestOtlpServer {get; private set;} = default!;

	public static async Task<DistributedApplication> StartAsync()
	{
		lock (OtlpServerLock)
		{
			TestOtlpServer ??= new TestOtlpServer();
		}

		IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Excos_Platform_AppHost>([
			"--environment=Testing"
			]);

		ConfigureOtlpExport(appHost);

		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});
		// To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

		DistributedApplication app = await appHost.BuildAsync();
		await app.StartAsync();

		return app;
	}

	private static void ConfigureOtlpExport(IDistributedApplicationTestingBuilder appHost)
	{
		// Configure OTLP exporter to send traces to the test OTLP server.
		appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = $"https://localhost:{TestOtlpServer.Port}";

		// Set a small batch schedule delay in development.
		// This reduces the delay that OTLP exporter waits to sends telemetry and reduces the need to wait in tests.
		const string value = "500"; // milliseconds
		appHost.Configuration["OTEL_BLRP_SCHEDULE_DELAY"] = value;
		appHost.Configuration["OTEL_BSP_SCHEDULE_DELAY"] = value;
		appHost.Configuration["OTEL_METRIC_EXPORT_INTERVAL"] = value;

		// Configure trace sampler to send all traces to the server.
		appHost.Configuration["OTEL_TRACES_SAMPLER"] = "always_on";
		// Configure metrics to include exemplars.
		appHost.Configuration["OTEL_METRICS_EXEMPLAR_FILTER"] = "trace_based";
	}

	public static async Task<HttpClient> GetWebApiClientAsync(this DistributedApplication app)
	{
		ResourceNotificationService resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
		await resourceNotificationService.WaitForResourceAsync("WebApiHost", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

		HttpClient httpClient = app.CreateHttpClient("WebApiHost");
		return httpClient;
	}
}