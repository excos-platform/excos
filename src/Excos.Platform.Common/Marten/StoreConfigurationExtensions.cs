// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.Common.Marten.Telemetry;
using Excos.Platform.Common.Privacy.Redaction;
using JasperFx.Core;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Weasel.Core;
using Wolverine.Marten;

namespace Excos.Platform.Common.Marten;

public static class StoreConfigurationExtensions
{
	public static IHostApplicationBuilder AddExcosMartenStore<TStore>(
		this IHostApplicationBuilder builder,
		string dbSchemaName,
		Action<StoreOptions>? configureOptions = null)
		where TStore : class, IDocumentStore
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dbSchemaName, nameof(dbSchemaName));

		builder.Services.AddExcosMartenStore<TStore>(builder.Environment, builder.Configuration, dbSchemaName, configureOptions);

		return builder;
	}

	public static IServiceCollection AddExcosMartenStore<TStore>(
		this IServiceCollection services,
		IHostEnvironment hostEnvironment,
		IConfiguration configuration,
		string dbSchemaName,
		Action<StoreOptions>? configureOptions = null)
		where TStore : class, IDocumentStore
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dbSchemaName, nameof(dbSchemaName));

		services.AddMartenStore<TStore>(provider =>
		{
			var options = new StoreOptions();

			options.Policies.AllDocumentsAreMultiTenanted();
			options.Policies.PartitionMultiTenantedDocumentsUsingMartenManagement("tenants");
			options.MultiTenantedWithSingleServer(configuration.GetConnectionString("postgres") ?? string.Empty);

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
			.IntegrateWithWolverine()
			// TODO: decide if ProcessEventsWithWolverineHandlersInStrictOrder would be better here
			.PublishEventsToWolverine(dbSchemaName, relay =>
			{
				relay.Options.SubscribeFromPresent();
			})
			// async deamon is needed to process projections, such as subscription based forwarding events to Wolverine
			.AddAsyncDaemon(hostEnvironment.IsDevelopment() ? DaemonMode.Solo : DaemonMode.HotCold);

		// TODO once we have a tenant context service we need to add tenant id to the session
		services.AddKeyedScoped<IDocumentSession>(dbSchemaName, (services, _) => services.GetRequiredService<TStore>().LightweightSession());

		return services;
	}
}
