// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Diagnostics;
using System.Text.Json;
using Excos.Platform.Common.Privacy.Redaction;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Configuration;

namespace Excos.Platform.Common.Wolverine.Telemetry
{
	public static class LoggingEventHandlerExtensions
	{
		public static HandlerDiscovery AddLogger<T>(this HandlerDiscovery discovery)
		{
			return discovery.IncludeType(typeof(LoggingEventHandler<T>));
		}
	}

	public static class LoggingEventHandler<T>
	{
		public static void Handle(T message, Envelope envelope, ILogger<T> logger, PrivacyValueRedactor redactor)
		{
			if (!logger.IsEnabled(LogLevel.Information) || message is null)
			{
				return;
			}

			var attributes = new ActivityTagsCollection()
			{
				{ "EnvelopeId", envelope.Id },
				{ "ConversationId", envelope.ConversationId == Guid.Empty ? null : envelope.ConversationId },
				// { "CorrelationId", envelope.CorrelationId }, this is just trace id so it's stamped on logs anyways
				{ "TenantId", envelope.TenantId },
				{ "Source", envelope.Source },
				{ "SentAt", envelope.SentAt },
				{ "SagaId", envelope.SagaId },
			};

			List<PrivacyValueDescriptor> privacyDescriptors = PrivacyValueDescriptor.GetDescriptors(typeof(T));
			var dataAttributes = new Dictionary<string, object?>(privacyDescriptors.Count);

			foreach (PrivacyValueDescriptor descriptor in privacyDescriptors)
			{
				dataAttributes.Add(descriptor.OpenTelemetryName, redactor.Redact(descriptor.GetValue(message), descriptor.Redaction));
			}

			attributes.Add("Data", JsonSerializer.Serialize(dataAttributes));

			logger.Log(
				LogLevel.Information,
				new EventId(0, typeof(T).FullNameInCode()),
				attributes,
				exception: null,
				static (attr, _) => $"{typeof(T).FullNameInCode()}: {attr["Data"]}");
		}
	}
}