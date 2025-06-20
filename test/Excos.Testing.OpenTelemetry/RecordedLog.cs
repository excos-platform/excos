// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;

namespace Excos.Testing.OpenTelemetry;

public class RecordedLog
{
	private readonly LogRecord log;

	internal RecordedLog(LogRecord log)
	{
		this.log = log;
	}

	public string Body => this.log.Body.StringValue ?? string.Empty;

	public string TraceId => this.log.TraceId.ToStringUtf8();

	public string SpanId => this.log.SpanId.ToStringUtf8();

	public IReadOnlyDictionary<string, object> Attributes => this.log.Attributes.ToDictionary(
		attr => attr.Key,
		attr => AttributeValue(attr.Value)
	);

	public LogLevel Severity => this.log.SeverityText switch
	{
		nameof(LogLevel.Debug) => LogLevel.Debug,
		nameof(LogLevel.Error) => LogLevel.Error,
		nameof(LogLevel.Information) => LogLevel.Information,
		nameof(LogLevel.Warning) => LogLevel.Warning,
		nameof(LogLevel.Trace) => LogLevel.Trace,
		nameof(LogLevel.Critical) => LogLevel.Critical,
		_ => LogLevel.None
	};

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