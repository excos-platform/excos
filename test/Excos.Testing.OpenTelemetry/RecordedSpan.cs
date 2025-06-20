// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Diagnostics;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Excos.Testing.OpenTelemetry;

public class RecordedSpan
{
	private readonly Span span;

	internal RecordedSpan(Span span)
	{
		this.span = span;
	}

	public string Name => this.span.Name;

	public string TraceId => Convert.ToHexStringLower(this.span.TraceId.ToByteArray());

	public string SpanId => Convert.ToHexStringLower(this.span.SpanId.ToByteArray());

	public string ParentSpanId => this.span.ParentSpanId.ToStringUtf8();

	public ActivityKind Kind => this.span.Kind switch
	{
		Span.Types.SpanKind.Client => ActivityKind.Client,
		Span.Types.SpanKind.Server => ActivityKind.Server,
		Span.Types.SpanKind.Producer => ActivityKind.Producer,
		Span.Types.SpanKind.Consumer => ActivityKind.Consumer,
		Span.Types.SpanKind.Internal => ActivityKind.Internal,
		_ => ActivityKind.Internal
	};

	public TimeSpan Duration => TimeSpan.FromMicroseconds((this.span.EndTimeUnixNano - this.span.StartTimeUnixNano) / 1_000);

	public ActivityStatusCode StatusCode => this.span.Status?.Code switch
	{
		Status.Types.StatusCode.Ok => ActivityStatusCode.Ok,
		Status.Types.StatusCode.Error => ActivityStatusCode.Error,
		_ => ActivityStatusCode.Unset
	};

	public string StatusMessage => this.span.Status?.Message ?? string.Empty;

	public IReadOnlyDictionary<string, object> Attributes => this.span.Attributes.ToDictionary(
		attr => attr.Key,
		attr => AttributeValue(attr.Value)
	);

	public IReadOnlyList<RecordedSpanEvent> Events => this.span.Events.Select(e => new RecordedSpanEvent(e, this)).ToList();

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