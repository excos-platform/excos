// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Asp.Versioning;
using Asp.Versioning.OData;
using Excos.Platform.Common.Privacy;
using Excos.Platform.Common.Wolverine.Telemetry;
using Excos.Platform.WebApiHost.Healthchecks;
using Excos.Platform.WebApiHost.OpenApi;
using Excos.Platform.WebApiHost.Telemetry;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Marten.Services;
using Marten.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData.ModelBuilder;
using Oakton;
using Weasel.Core;
using Wolverine;
using Wolverine.Marten;

namespace Excos.Platform.WebApiHost;

public static class ProgramConfiguration
{
	// Services configuration
	public static void ConfigureServices(WebApplicationBuilder builder)
	{
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

		// Generic Marten Store needed for some dependencies and configuring Wolverine correctly
		builder.Services.AddMarten(options =>
		{
			options.Events.TenancyStyle = TenancyStyle.Conjoined;
			options.Policies.AllDocumentsAreMultiTenanted();

			// in order for tenancy style Conjoined to be used over a single database we need to use Connection method
			options.Connection(builder.Configuration.GetConnectionString("postgres") ?? string.Empty);

			options.DatabaseSchemaName = "excos";

			options.OpenTelemetry.TrackConnections = TrackLevel.Normal;
			options.OpenTelemetry.TrackEventCounters();

			options.UseSystemTextJsonForSerialization();

			// FUTURE: In the future we may want to turn this off in production and execute migrations in a separate process
			options.AutoCreateSchemaObjects = AutoCreate.All;

			// FUTURE: Change to Guid once we move on from example code.
			options.Events.StreamIdentity = StreamIdentity.AsString;

			// START PROJECTIONS
			options.Projections.Snapshot<Counter>(SnapshotLifecycle.Inline);
			// END PROJECTIONS
		})
			.IntegrateWithWolverine()
			// TODO: decide if ProcessEventsWithWolverineHandlersInStrictOrder would be better here
			.PublishEventsToWolverine("marten", relay =>
			{
				relay.Options.SubscribeFromPresent();
			})
			// async deamon is needed to process projections, such as subscription based forwarding events to Wolverine
			.AddAsyncDaemon(builder.Environment.IsDevelopment() ? DaemonMode.Solo : DaemonMode.HotCold);

		// TODO once we have a tenant context service we need to add tenant id to the session
		builder.Services.AddScoped<IDocumentSession>((services) => services.GetRequiredService<IDocumentStore>().LightweightSession("TODO:DEFAULT"));

		builder.Services.AddWolverine(options =>
		{
			options.Policies.Add<EventLoggingPolicy>();

			options.Policies.UseDurableLocalQueues();

			options.Policies.AutoApplyTransactions();

			options.Discovery
				.AddLogger<IncreaseCounterCommand>()
				.AddLogger<CounterIncreased>();
		});

		builder.Services.AddControllers()
			.AddOData(options =>
			{
				options.EnableQueryFeatures();
				options.RouteOptions.EnableQualifiedOperationCall = false;
				options.RouteOptions.EnableKeyAsSegment = false;
			});
		builder.Services.AddProblemDetails();
		builder.Services.AddApiVersioning(options =>
		{
			options.AssumeDefaultVersionWhenUnspecified = true;
			options.ApiVersionReader = new QueryStringApiVersionReader();

			// allow reporting of api versions via OPTIONS call
			options.ReportApiVersions = true;
		})
			.AddOData(options =>
			{
				options.ModelBuilder.DefaultModelConfiguration = (builder, apiVersion, routePrefix) =>
				{
					builder.Namespace = "Excos";
					EntityTypeConfiguration<Counter> counters = builder.EntitySet<Counter>("Counters").EntityType;
					counters.Action("Increase");
				};
				options.AddRouteComponents("api");
			})
			.AddODataApiExplorer(options =>
			{
				options.GroupNameFormat = "'v'VVV";
				options.SubstituteApiVersionInUrl = true;
			});

		// for each version of API
		builder.Services.AddOpenApiDocument(document =>
		{
			document.DocumentName = "v1";
			document.ApiGroupNames = ["v1"];
			document.OperationProcessors.Add(new ODataOperationProcessor());
		});
	}

	// AspNet middleware configuration
	public static void ConfigureMiddleware(WebApplication app)
	{
		// Generate ProblemDetails for unhandled exceptions and non-successful status codes
		app.UseExceptionHandler();
		app.UseStatusCodePages();

		// these are used by health check endpoints
		app.UseRequestTimeouts();
		app.UseOutputCache();

		app.UseVersionedODataBatching();
		app.UseOpenApi();

		MapApplicationEndpoints(app);
		app.MapControllers();
		app.MapHealthCheckEndpoints();

		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			// Access ~/$odata to identify OData endpoints that failed to match a route template
			app.UseODataRouteDebug();
			// Access ~/swagger to see the OpenAPI documentation
			app.UseSwaggerUi();
		}
	}

	private static void MapApplicationEndpoints(WebApplication app)
	{
		app.MapGet("/", () => "Hello World!");
	}
}