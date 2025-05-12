// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.Extensions.Logging;

namespace Excos.Testing.OpenTelemetry.Asserts;

public class LogAssert
{
	private readonly TelemetryAssert parent;
	private readonly IEnumerable<RecordedLog> logs;

	public LogAssert(TelemetryAssert parent, IEnumerable<RecordedLog> logs)
	{
		this.parent = parent;
		this.logs = logs;
	}

	public IEnumerable<RecordedLog> Logs => this.logs;

	public LogAssert WithAttributes(params (string Key, object Value)[] attributes)
	{
		var matchingLogs = this.logs.Where(log =>
			attributes.All(attr => log.Attributes.ContainsKey(attr.Key) && log.Attributes[attr.Key] == attr.Value))
			.ToList();
		if (matchingLogs.Count == 0)
		{
			throw new AssertException($"Expected attribute set not present in any log.");
		}

		return new LogAssert(this.parent, matchingLogs);
	}

	public LogAssert WithAttributeKeys(params string[] attributeKeys)
	{
		var matchingLogs = this.logs.Where(log =>
			attributeKeys.All(attrKey => log.Attributes.ContainsKey(attrKey)))
			.ToList();
		if (matchingLogs.Count == 0)
		{
			throw new AssertException($"Expected attribute keys not present in any log.");
		}

		return new LogAssert(this.parent, matchingLogs);
	}

	public LogAssert WithSeverity(LogLevel severity)
	{
		var matchingLogs = this.logs.Where(log => log.Severity == severity).ToList();
		if (matchingLogs.Count == 0)
		{
			throw new AssertException($"Expected log severity not present in any log.");
		}

		return new LogAssert(this.parent, matchingLogs);
	}

	public TelemetryAssert And()
	{
		return this.parent;
	}
}