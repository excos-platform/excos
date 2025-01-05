// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Runtime;

namespace Excos.Platform.Common.Wolverine.Telemetry
{
	public static class LoggingMessageBusDIExtension
	{
		public static IServiceCollection AddLoggingMessageBus(this IServiceCollection services)
		{
			services.AddScoped<MessageContext>();
			services.AddScoped<LoggingMessageBus>();
			services.AddScoped<IMessageBus>(provider => provider.GetRequiredService<LoggingMessageBus>());
			services.AddScoped<IMessageContext>(provider => provider.GetRequiredService<LoggingMessageBus>());
			return services;
		}
	}
	
	internal class LoggingMessageBus : IMessageContext
	{
		private readonly MessageContext inner;
		private readonly ILogger<LoggingMessageBus> logger;

		public LoggingMessageBus(MessageContext inner, ILogger<LoggingMessageBus> logger)
		{
			this.inner = inner;
			this.logger = logger;
		}

		public string? TenantId
		{
			get => this.inner.TenantId;
			set => this.inner.TenantId = value;
		}

		public string? CorrelationId
		{
			get => this.inner.CorrelationId; 
			set => this.inner.CorrelationId = value; 
		}

		public Envelope? Envelope => this.inner.Envelope;

		public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
		{
			this.logger.LogInformation("Broadcasting to topic {TopicName} with message {Message}", topicName, message);
			return this.inner.BroadcastToTopicAsync(topicName, message, options);
		}

		public IDestinationEndpoint EndpointFor(string endpointName)
		{
			this.logger.LogInformation("Getting endpoint for {EndpointName}", endpointName);
			return this.inner.EndpointFor(endpointName);
		}

		public IDestinationEndpoint EndpointFor(Uri uri)
		{
			this.logger.LogInformation("Getting endpoint for {Uri}", uri);
			return this.inner.EndpointFor(uri);
		}

		public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
		{
			this.logger.LogInformation("Invoking message {Message}", message);
			return this.inner.InvokeAsync(message, cancellation, timeout);
		}

		public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
		{
			this.logger.LogInformation("Invoking message {Message} with return type {ReturnType}", message, typeof(T));
			return this.inner.InvokeAsync<T>(message, cancellation, timeout);
		}

		public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
		{
			this.logger.LogInformation("Invoking message {Message} for tenant {TenantId}", message, tenantId);
			return this.inner.InvokeForTenantAsync(tenantId, message, cancellation, timeout);
		}

		public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
		{
			this.logger.LogInformation("Invoking message {Message} for tenant {TenantId} with return type {ReturnType}", message, tenantId, typeof(T));
			return this.inner.InvokeForTenantAsync<T>(tenantId, message, cancellation, timeout);
		}

		public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
		{
			this.logger.LogInformation("Previewing subscriptions for message {Message}", message);
			return this.inner.PreviewSubscriptions(message);
		}

		public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
		{
			this.logger.LogInformation("Publishing message {Message}", message);
			return this.inner.PublishAsync(message, options);
		}

		public ValueTask RespondToSenderAsync(object response)
		{
			throw new NotImplementedException();
		}

		public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
		{
			this.logger.LogInformation("Sending message {Message}", message);
			return this.inner.SendAsync(message, options);
		}

		public static explicit operator MessageBus(LoggingMessageBus bus)
		{
			return bus.inner;
		}

		public static explicit operator MessageContext(LoggingMessageBus bus)
		{
			return bus.inner;
		}
	}
}
