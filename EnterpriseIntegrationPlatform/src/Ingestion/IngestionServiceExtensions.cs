using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Extension methods for registering the configurable message broker layer.
/// The broker is selected at deployment time via the <c>Broker</c> configuration section.
/// </summary>
public static class IngestionServiceExtensions
{
    /// <summary>
    /// Registers <see cref="BrokerOptions"/> from the <c>Broker</c> configuration section.
    /// Downstream provider registration methods (e.g. <c>AddNatsJetStreamBroker</c>,
    /// <c>AddKafkaBroker</c>, <c>AddPulsarBroker</c>) use these options to wire the
    /// correct <see cref="IMessageBrokerProducer"/> and <see cref="IMessageBrokerConsumer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBrokerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BrokerOptions>(
            configuration.GetSection(BrokerOptions.SectionName));

        return services;
    }
}
