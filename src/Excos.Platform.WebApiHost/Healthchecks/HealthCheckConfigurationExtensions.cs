// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Excos.Platform.WebApiHost.Healthchecks;

public static class HealthCheckConfigurationExtensions
{
	public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
	{
		builder.Services.AddRequestTimeouts(
		configure: static timeouts =>
			timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(2)));

		builder.Services.AddOutputCache(
			configureOptions: static caching =>
				caching.AddPolicy("HealthChecks",
				build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

		builder.Services.AddHealthChecks()
			// Add a default liveness check to ensure app is responsive
			.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		return builder;
	}

	public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
	{
		// Adding health checks endpoints to applications in non-development environments has security implications.
		// The below code is copied from https://aka.ms/dotnet/aspire/healthchecks based on their recommendations.
		RouteGroupBuilder healthChecks = app.MapGroup("");

		healthChecks
			.CacheOutput("HealthChecks")
			.WithRequestTimeout("HealthChecks");

		// All health checks must pass for app to be
		// considered ready to accept traffic after starting
		healthChecks.MapHealthChecks("/health");

		// Only health checks tagged with the "live" tag
		// must pass for app to be considered alive
		healthChecks.MapHealthChecks("/alive", new()
		{
			Predicate = static r => r.Tags.Contains("live")
		});

		return app;
	}
}