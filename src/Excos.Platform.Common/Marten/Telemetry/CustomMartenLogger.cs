// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Diagnostics;
using Excos.Platform.Common.Privacy.Redaction;
using JasperFx.Core;
using Marten;
using Marten.Services;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Excos.Platform.Common.Marten.Telemetry;

/// <summary>
/// Custom Marten logger which populates the current activity with the event data.
/// </summary>
/// <param name="Redactor">Privacy redactor.</param>
/// <param name="Inner">Logger for error messages.</param>
internal class CustomMartenLogger(PrivacyValueRedactor Redactor, ILogger Inner) : IMartenLogger, IMartenSessionLogger
{
	public IMartenSessionLogger StartSession(IQuerySession session) => this;

	public void SchemaChange(string sql)
	{
		if (Inner.IsEnabled(LogLevel.Information))
		{
			Inner.LogInformation("Executed schema update SQL:\n{SQL}", sql);
		}
	}

	public void LogSuccess(NpgsqlCommand command)
	{
	}

	public void LogSuccess(NpgsqlBatch batch)
	{
	}

	public void LogFailure(NpgsqlCommand command, Exception ex)
	{
		string message = "Marten encountered an exception executing \n{SQL}\n{PARAMS}";
		string parameters = command.Parameters.OfType<NpgsqlParameter>()
			.Select(p => $"  {p.ParameterName}: {p.Value}")
			.Join(Environment.NewLine);
		Inner.LogError(ex, message, command.CommandText, parameters);
	}

	public void LogFailure(NpgsqlBatch batch, Exception ex)
	{
		string message = "Marten encountered an exception executing \n{SQL}\n{PARAMS}";

		foreach (System.Data.Common.DbBatchCommand command in batch.BatchCommands)
		{
			string parameters = command.Parameters.OfType<NpgsqlParameter>()
				.Select(p => $"  {p.ParameterName}: {p.Value}")
				.Join(Environment.NewLine);
			Inner.LogError(ex, message, command.CommandText, parameters);
		}
	}

	public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
	{
		if (Activity.Current != null)
		{
			foreach (global::Marten.Events.IEvent evnt in commit.GetEvents())
			{
				List<PrivacyValueDescriptor> privacyDescriptors = PrivacyValueDescriptor.GetDescriptors(evnt.Data.GetType());
				var eventTags = new ActivityTagsCollection
				{
					{ "_EventVersion", evnt.Version },
					{ "_EventSequence", evnt.Sequence }
				};

				foreach (PrivacyValueDescriptor descriptor in privacyDescriptors)
				{
					eventTags.Add(descriptor.OpenTelemetryName, Redactor.Redact(descriptor.GetValue(evnt.Data), descriptor.Redaction));
				}

				Activity.Current.AddEvent(new ActivityEvent(PrivacyValueDescriptor.GetDisplayName(evnt.EventType), tags: eventTags));
			}
		}
	}

	public void OnBeforeExecute(NpgsqlCommand command)
	{
	}

	public void LogFailure(Exception ex, string message)
	{
		Inner.LogError(ex, message);
	}

	public void OnBeforeExecute(NpgsqlBatch batch)
	{
	}
}