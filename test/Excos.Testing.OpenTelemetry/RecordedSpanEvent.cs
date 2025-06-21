// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Excos.Testing.OpenTelemetry;

public class RecordedSpanEvent
{
	private readonly Span.Types.Event @event;
	private readonly RecordedSpan parent;

	internal RecordedSpanEvent(Span.Types.Event evt, RecordedSpan parent)
	{
		this.@event = evt;
		this.parent = parent;
	}

	public string Name => this.@event.Name;

	public RecordedSpan Parent => this.parent;

	public IReadOnlyDictionary<string, object> Attributes => this.@event.Attributes.ToDictionary(
		attr => attr.Key,
		attr => AttributeValue(attr.Value)
	);

	private static object AttributeValue(AnyValue value)
	{
		return value.ValueCase switch
		{
			AnyValue.ValueOneofCase.StringValue => value.StringValue,
			AnyValue.ValueOneofCase.IntValue => value.IntValue,
			AnyValue.ValueOneofCase.BoolValue => value.BoolValue,
			AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue,
			_ => value.ToString()
		};
	}
}