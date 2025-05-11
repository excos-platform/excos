// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Testing.OpenTelemetry.Asserts;

public class SpanAssert
{
	private readonly TelemetryAssert parent;
	private readonly IEnumerable<RecordedSpan> spans;

	public SpanAssert(TelemetryAssert parent, IEnumerable<RecordedSpan> spans)
	{
		this.parent = parent;
		this.spans = spans;
	}

	public IEnumerable<RecordedSpan> Spans => this.spans;

	public SpanAssert WithAttributes(params (string Key, object Value)[] attributes)
	{
		var matchingSpans = this.spans.Where(span =>
			attributes.All(attr => span.Attributes.ContainsKey(attr.Key) && span.Attributes[attr.Key] == attr.Value))
			.ToList();
		if (matchingSpans.Count == 0)
		{
			throw new AssertException($"Expected attribute set not present in any span.");
		}

		return new SpanAssert(this.parent, matchingSpans);
	}

	public SpanAssert WithAttributeKeys(params string[] attributeKeys)
	{
		var matchingSpans = this.spans.Where(span =>
			attributeKeys.All(attrKey => span.Attributes.ContainsKey(attrKey)))
			.ToList();
		if (matchingSpans.Count == 0)
		{
			throw new AssertException($"Expected attribute keys not present in any span.");
		}

		return new SpanAssert(this.parent, matchingSpans);
	}

	public SpanEventAssert WithEvent(string eventName)
	{
		var matchingEvents = this.spans.Where(span => span.Events.Any(e => e.Name == eventName)).SelectMany(span => span.Events).ToList();
		if (matchingEvents.Count == 0)
		{
			throw new AssertException($"Expected event '{eventName}' not present in any span.");
		}

		return new SpanEventAssert(this.parent, matchingEvents);
	}

	public TelemetryAssert And()
	{
		return this.parent;
	}
}