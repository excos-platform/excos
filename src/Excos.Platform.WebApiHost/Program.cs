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

WebApplication app = builder.Build();

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
//---------
// TEST CODE BELOW
static void MapApplicationEndpoints(WebApplication app)
{
	app.MapGet("/", () => "Hello World!");
}

[ApiVersion(1.0)]
public class CountersController : ODataController
{
	private readonly IDocumentStore store;
	private readonly IMessageBus messageBus;
	public CountersController(IDocumentStore store, IMessageBus messageBus)
	{
		this.store = store;
		this.messageBus = messageBus;
	}

	[HttpGet]
	[EnableQuery]
	[ProducesResponseType(typeof(ODataValue<IEnumerable<Counter>>), StatusCodes.Status200OK)]
	public IActionResult Get()
	{
		IDocumentSession session = this.store.LightweightSession("DEFAULT");
		IQueryable<Counter> counters = session.Query<Counter>();
		return this.Ok(counters);
	}

	[HttpGet]
	[EnableQuery]
	[ProducesResponseType(typeof(Counter), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Get(string key)
	{
		IDocumentSession session = this.store.LightweightSession("DEFAULT");
		Counter? counter = await session.LoadAsync<Counter>(key);
		if (counter == null)
		{
			return this.NotFound();
		}
		return this.Ok(counter);
	}

	[HttpPost]
	[ProducesResponseType(typeof(ODataValue<string>), StatusCodes.Status200OK)]
	public async Task<IActionResult> Increase(string key)
	{
		await this.messageBus.InvokeForTenantAsync("DEFAULT", new IncreaseCounterCommand(key));
		return this.Ok("Counter increased");
	}
}

public record IncreaseCounterCommand([property: UPI] string CounterId);
public record CounterIncreased([property: UPI] string CounterId);

public class Counter
{
	[UPI]
	public string Id { get; set; } = default!;
	public int Value { get; set; } = 0;

	public void Apply(CounterIncreased _)
	{
		this.Value += 1;
	}
}

public static class IncreaseCounterCommandHandler
{
	[AggregateHandler(AggregateType = typeof(Counter))]
	public static CounterIncreased Handle(IncreaseCounterCommand command)
	{
		return new CounterIncreased(command.CounterId);
	}
}

public partial class Program { }