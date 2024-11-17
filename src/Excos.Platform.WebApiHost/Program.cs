// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.WebApiHost.Healthchecks;
using Excos.Platform.WebApiHost.Telemetry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

WebApplication app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapDevHealthCheckEndpoints();

app.Run();