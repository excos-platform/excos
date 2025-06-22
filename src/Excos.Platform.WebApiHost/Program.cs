// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.WebApiHost;
using Excos.Platform.WebApiHost.Telemetry;
using Oakton;

// Executable entry point
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure services
ProgramConfiguration.ConfigureServices(builder);

WebApplication app = builder.Build();

// Configure middleware pipeline
ProgramConfiguration.ConfigureMiddleware(app);

try
{
	// In order to observe startup exceptions (such as when resolving an IHostedService)
	// we will be running the host on its own outside of development.
	// Oakton logs exception to console and does not rethrow it.
	if (app.Environment.IsDevelopment())
	{
		return await app.RunOaktonCommands(args);
	}

	await app.RunAsync();
	return 0;
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.ToString());

	try
	{
		// Try to log the exception via open telemetry
		HostApplicationBuilder emergencyHostBuilder = Host.CreateEmptyApplicationBuilder(new());
		emergencyHostBuilder.ConfigureOpenTelemetry();

		using (IHost emergencyHost = emergencyHostBuilder.Build())
		{
			emergencyHost.Services.GetRequiredService<ILogger<Program>>().LogCritical(ex, "Host terminated unexpectedly");
		}
	}
	finally { }

	return 1;
}

// Make Program class accessible to WebApplicationFactory
public partial class Program { }