using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Extension methods for registering the NATS JetStream message broker provider.
/// </summary>
public static class NatsServiceExtensions
{
    /// <summary>
    /// Registers the NATS JetStream message broker producer and consumer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// NATS server URL, e.g. <c>nats://localhost:4222</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsJetStreamBroker(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<INatsConnection>(_ =>
            new NatsConnection(new NatsOpts { Url = connectionString }));

        services.AddSingleton<IMessageBrokerProducer, NatsJetStreamProducer>();
        services.AddSingleton<IMessageBrokerConsumer, NatsJetStreamConsumer>();

        return services;
    }
}
