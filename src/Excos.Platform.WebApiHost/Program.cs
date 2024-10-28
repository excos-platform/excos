using Excos.Platform.WebApiHost.Healthchecks;
using Excos.Platform.WebApiHost.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOpenTelemetry();

builder.AddDefaultHealthChecks();

builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
	// Turn on resilience by default
	http.AddStandardResilienceHandler();

	// Turn on service discovery by default
	http.AddServiceDiscovery();
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapDevHealthCheckEndpoints();

app.Run();