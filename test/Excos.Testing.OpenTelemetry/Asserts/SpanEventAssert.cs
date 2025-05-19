// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Testing.OpenTelemetry.Asserts;

public class SpanEventAssert
{
	private readonly TelemetryAssert parent;
	private readonly IEnumerable<RecordedSpanEvent> spanEvents;

	public SpanEventAssert(TelemetryAssert parent, IEnumerable<RecordedSpanEvent> spanEvents)
	{
		this.parent = parent;
		this.spanEvents = spanEvents;
	}

	public IEnumerable<RecordedSpanEvent> Events => this.spanEvents;

	public SpanEventAssert WithAttributes(params (string Key, object Value)[] attributes)
	{
		var matchingSpanEvents = this.spanEvents.Where(spanEvent =>
			attributes.All(attr => spanEvent.Attributes.ContainsKey(attr.Key) && spanEvent.Attributes[attr.Key] == attr.Value))
			.ToList();
		if (matchingSpanEvents.Count == 0)
		{
			throw new AssertException($"Expected attribute set not present in any span event.");
		}

		return new SpanEventAssert(this.parent, matchingSpanEvents);
	}

	public SpanEventAssert WithAttributeKeys(params string[] attributeKeys)
	{
		var matchingSpanEvents = this.spanEvents.Where(spanEvent =>
			attributeKeys.All(attrKey => spanEvent.Attributes.ContainsKey(attrKey)))
			.ToList();
		if (matchingSpanEvents.Count == 0)
		{
			throw new AssertException($"Expected attribute keys not present in any span event.");
		}

		return new SpanEventAssert(this.parent, matchingSpanEvents);
	}

	public TelemetryAssert And()
	{
		return this.parent;
	}
}