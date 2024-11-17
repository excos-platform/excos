// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Excos.Platform.WebApiHost.Healthchecks;

public static class HealthCheckConfigurationExtensions
{
	public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
	{
		builder.Services.AddHealthChecks()
			// Add a default liveness check to ensure app is responsive
			.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		return builder;
	}

	public static WebApplication MapDevHealthCheckEndpoints(this WebApplication app)
	{
		// Adding health checks endpoints to applications in non-development environments has security implications.
		// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
		if (app.Environment.IsDevelopment())
		{
			// All health checks must pass for app to be considered ready to accept traffic after starting
			app.MapHealthChecks("/health");

			// Only health checks tagged with the "live" tag must pass for app to be considered alive
			app.MapHealthChecks("/alive", new HealthCheckOptions
			{
				Predicate = r => r.Tags.Contains("live")
			});
		}

		return app;
	}
}