// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.Common.Marten.Telemetry;
using Excos.Platform.Common.Privacy.Redaction;
using Marten;
using Marten.Events;
using Marten.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Weasel.Core;
using Wolverine.Marten;

namespace Excos.Platform.Common.Marten;

public static class StoreConfigurationExtensions
{
	public static IServiceCollection AddExcosMartenStore<TStore>(this IServiceCollection services, IConfiguration configuration, string dbSchemaName, Action<StoreOptions>? configureOptions = null)
		where TStore : class, IDocumentStore
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dbSchemaName, nameof(dbSchemaName));

		services.AddMartenStore<TStore>(provider =>
		{
			var options = new StoreOptions();
			options.Connection(configuration.GetConnectionString("postgres") ?? string.Empty);
			options.Events.DatabaseSchemaName = dbSchemaName;
			
			options.OpenTelemetry.TrackConnections = TrackLevel.Normal;
			options.OpenTelemetry.TrackEventCounters();

			options.UseSystemTextJsonForSerialization();

			// FUTURE: In the future we may want to turn this off in production and execute migrations in a separate process
			options.AutoCreateSchemaObjects = AutoCreate.All;

			options.Events.StreamIdentity = StreamIdentity.AsString;

			options.Logger(new CustomMartenLogger(provider.GetRequiredService<PrivacyValueRedactor>(), provider.GetRequiredService<ILogger<TStore>>()));

			configureOptions?.Invoke(options);

			return options;
		})
			.IntegrateWithWolverine();

		services.AddKeyedScoped<IDocumentSession>(dbSchemaName, (services, _) => services.GetRequiredService<TStore>().LightweightSession());

		return services;
	}
}
