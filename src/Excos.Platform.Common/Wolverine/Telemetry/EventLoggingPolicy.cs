// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using JasperFx.CodeGeneration;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace Excos.Platform.Common.Wolverine.Telemetry;

/// <summary>
/// Add event fields logging on the current activity for all event handlers.
/// </summary>
public class EventLoggingPolicy : IHandlerPolicy
{
	public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
	{
		foreach (HandlerChain chain in chains)
		{
			chain.Middleware.Add(new EventLoggingFrame(chain));
		}
	}
}