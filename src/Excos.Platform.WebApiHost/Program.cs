// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.Common.Marten;
using Excos.Platform.Common.Privacy;
using Excos.Platform.Common.Privacy.Redaction;
using Excos.Platform.Common.Wolverine.Telemetry;
using Excos.Platform.WebApiHost.Healthchecks;
using Excos.Platform.WebApiHost.Telemetry;
using Marten;
using Marten.Services;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddMarten(options =>
{
	options.Connection(builder.Configuration.GetConnectionString("postgres") ?? string.Empty);
	options.OpenTelemetry.TrackConnections = TrackLevel.Normal;

	options.UseSystemTextJsonForSerialization();

	// FUTURE: In the future we may want to turn this off in production and execute migrations in a separate process
	options.AutoCreateSchemaObjects = AutoCreate.All;
})
	.IntegrateWithWolverine()
	.UseLightweightSessions();

builder.AddExcosMartenStore<ICounterStore>("counters");

builder.Services.AddSingleton<PrivacyValueRedactor>();

builder.Services.AddWolverine(options =>
{
	options.Policies.Add<EventLoggingPolicy>();

	options.Policies.UseDurableLocalQueues();

	options.Policies.AutoApplyTransactions();

	options.Discovery
		.AddLogger<IncreaseCounterCommand>()
		.AddLogger<CounterIncreased>();
});

WebApplication app = builder.Build();

MapApplicationEndpoints(app);

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

static void MapApplicationEndpoints(WebApplication app)
{
	app.MapGet("/", () => "Hello World!");
	app.MapGet("/counter/{id}", async ([FromRoute] string id, [FromQuery] string? tenantId, ICounterStore store) =>
	{
		IDocumentSession session = tenantId != null ? store.LightweightSession(tenantId) : store.LightweightSession();
		Counter? counter = await session.Events.AggregateStreamAsync<Counter>(id);
		return counter?.Value ?? 0;
	});
	app.MapPost("/counter/{id}/increase", async ([FromRoute] string id, [FromQuery] string? tenantId, IMessageBus bus) =>
	{
		if (tenantId != null)
		{
			await bus.InvokeForTenantAsync(tenantId, new IncreaseCounterCommand(id));
		}
		else
		{
			await bus.InvokeAsync(new IncreaseCounterCommand(id));
		}
		return "Counter increased";
	});
	app.MapDevHealthCheckEndpoints();
}

public interface ICounterStore : IDocumentStore;
public record IncreaseCounterCommand([property: UPI] string CounterId);
public record CounterIncreased([property: UPI] string CounterId);

public class Counter
{
	[UPI]
	public string Id { get; set; } = default!;
	public int Value { get; set; }

	public void Apply(CounterIncreased _)
	{
		this.Value++;
	}
}

[MartenStore(typeof(ICounterStore))]
public static class IncreaseCounterCommandHandler
{
	[AggregateHandler(AggregateType = typeof(Counter))]
	public static CounterIncreased Handle(IncreaseCounterCommand command)
	{
		return new CounterIncreased(command.CounterId);
	}
}