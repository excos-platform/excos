// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Diagnostics;
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

builder.Services.AddExcosMartenStore<ICounterStore>(builder.Configuration, "counters");

builder.Services.AddSingleton<PrivacyValueRedactor>();

builder.Services.AddWolverine(options =>
{
	options.Policies.Add<EventLoggingPolicy>();
});

WebApplication app = builder.Build();

var idBase = Guid.Parse("60623ee8-6f9a-45b9-840e-09d7560b4643");

app.MapGet("/", () => "Hello World!");
app.MapGet("/counter/{id}", async ([FromRoute] string id, ICounterStore store) =>
{
	IDocumentSession session = store.LightweightSession();
	Counter? counter = await session.Events.AggregateStreamAsync<Counter>(id);
	return counter?.Value ?? 0;
});
app.MapPost("/counter/{id}/increase", async ([FromRoute] string id, IMessageBus bus) =>
{
	Activity.Current?.AddEvent(new ActivityEvent("IncreaseCounter", tags: [System.Collections.Generic.KeyValuePair.Create("Id", (object?)id)]));
	await bus.InvokeAsync(new IncreaseCounterCommand(id));
	return "Counter increased";
});
app.MapDevHealthCheckEndpoints();

return await app.RunOaktonCommands(args);

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