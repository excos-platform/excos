// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Testing.OpenTelemetry.Asserts;

public static class TelemetryAssertions
{
	public static TelemetryAssert Should(this TestOtlpServer server)
	{
		return new TelemetryAssert(server);
	}
}

public class TelemetryAssert
{
	private readonly TestOtlpServer server;

	public TelemetryAssert(TestOtlpServer server)
	{
		this.server = server;
	}

	protected virtual IEnumerable<RecordedSpan> GetSpans()
	{
		return this.server.GetSpans();
	}

	protected virtual IEnumerable<RecordedLog> GetLogs()
	{
		return this.server.GetLogs();
	}

	public SpanAssert HaveSpan(string spanName)
	{
		IEnumerable<RecordedSpan> spans = this.GetSpans().Where(s => s.Name == spanName);
		if (spans.Count() > 0)
			return new SpanAssert(this, spans);

		throw new AssertException($"Span '{spanName}' was not received at this time.");
	}

	public LogAssert HaveLog(string text)
	{
		IEnumerable<RecordedLog> logs = this.GetLogs().Where(l => l.Body.Contains(text, StringComparison.OrdinalIgnoreCase));
		if (logs.Count() > 0)
			return new LogAssert(this, logs);

		throw new AssertException($"Log with body '{text}' was not received at this time.");
	}

	public TelemetryAssert WithTraceId(string traceId)
	{
		return new TracedTelemetryAssert(this.server, traceId);
	}
}

internal class TracedTelemetryAssert(TestOtlpServer server, string traceId) : TelemetryAssert(server)
{
	protected override IEnumerable<RecordedSpan> GetSpans()
	{
		return base.GetSpans().Where(s => s.TraceId == traceId);
	}

	protected override IEnumerable<RecordedLog> GetLogs()
	{
		return base.GetLogs().Where(l => l.TraceId == traceId);
	}
}