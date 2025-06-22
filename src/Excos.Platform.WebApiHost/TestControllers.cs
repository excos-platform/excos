// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Asp.Versioning;
using Excos.Platform.ApiClient.V1;
using Excos.Platform.Common.Privacy;
using Marten;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData.ModelBuilder;
using Wolverine;
using Wolverine.Marten;

namespace Excos.Platform.WebApiHost;

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
	[ProducesResponseType(typeof(ODataValueOfIEnumerableOfCounter), StatusCodes.Status200OK)]
	public IActionResult Get()
	{
		IDocumentSession session = this.store.LightweightSession("DEFAULT");
		IQueryable<Counter> counters = session.Query<Counter>();
		return this.Ok(counters);
	}

	[HttpGet]
	[EnableQuery]
	[ProducesResponseType(typeof(Counter), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
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
	[ProducesResponseType(typeof(ODataValueOfString), StatusCodes.Status200OK)]
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