using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    /// NATS server URL, e.g. <c>nats://localhost:15222</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsJetStreamBroker(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.Configure<NatsOptions>(opts => opts.Url = connectionString);

        services.AddSingleton<INatsConnection>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<NatsOptions>>().Value;
            opts.Validate();
            return new NatsConnection(new NatsOpts { Url = opts.Url });
        });

        services.AddSingleton<IMessageBrokerProducer, NatsJetStreamProducer>();
        services.AddSingleton<IMessageBrokerConsumer, NatsJetStreamConsumer>();
        services.AddSingleton<NatsHealthCheck>();

        return services;
    }
}
