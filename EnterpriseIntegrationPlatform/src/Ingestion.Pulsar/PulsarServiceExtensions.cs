using DotPulsar;
using DotPulsar.Abstractions;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Extension methods for registering the Apache Pulsar message broker provider.
/// </summary>
public static class PulsarServiceExtensions
{
    /// <summary>
    /// Registers the Apache Pulsar message broker producer and consumer
    /// with Key_Shared subscription for recipient-keyed distribution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceUrl">
    /// Pulsar service URL, e.g. <c>pulsar://localhost:6650</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPulsarBroker(
        this IServiceCollection services,
        string serviceUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl);

        services.Configure<PulsarOptions>(opts => opts.ServiceUrl = serviceUrl);

        services.AddSingleton<IPulsarClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PulsarOptions>>().Value;
            opts.Validate();
            return PulsarClient.Builder()
                .ServiceUrl(new Uri(opts.ServiceUrl))
                .Build();
        });

        services.AddSingleton<IMessageBrokerProducer, PulsarProducer>();
        services.AddSingleton<IMessageBrokerConsumer, PulsarConsumer>();
        services.AddSingleton<PulsarHealthCheck>();

        return services;
    }
}
