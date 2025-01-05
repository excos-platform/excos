// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.Extensions.Logging;
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
		public static void Handle(T message, ILogger<T> logger)
		{
			logger.LogInformation("LEH> Handling message {Message}", message);
		}
	}
}
