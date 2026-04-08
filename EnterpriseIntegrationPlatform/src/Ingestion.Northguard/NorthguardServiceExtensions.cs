using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion.Northguard;

/// <summary>
/// Extension methods for registering the Northguard message broker provider.
/// </summary>
public static class NorthguardServiceExtensions
{
    /// <summary>
    /// Registers the Northguard message broker producer and consumer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceUrl">
    /// Northguard service URL, e.g. <c>https://northguard.example.com</c>.
    /// This should point to the Xinfra gateway or direct Northguard API endpoint.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNorthguardBroker(
        this IServiceCollection services,
        string serviceUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl);

        services.AddSingleton(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(serviceUrl),
                Timeout = TimeSpan.FromSeconds(60),
            };
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
            return client;
        });

        services.AddSingleton<IMessageBrokerProducer, NorthguardProducer>();
        services.AddSingleton<IMessageBrokerConsumer, NorthguardConsumer>();

        return services;
    }
}
