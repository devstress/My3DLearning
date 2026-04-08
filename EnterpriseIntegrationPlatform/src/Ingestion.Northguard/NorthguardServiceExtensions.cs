using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion.Northguard;

/// <summary>
/// Extension methods for registering the Northguard message broker provider.
/// </summary>
public static class NorthguardServiceExtensions
{
    /// <summary>
    /// Client name used for <see cref="IHttpClientFactory"/> registration.
    /// </summary>
    internal const string HttpClientName = "Northguard";

    /// <summary>
    /// Registers the Northguard message broker producer and consumer.
    /// Uses <see cref="IHttpClientFactory"/> for proper connection pooling and DNS refresh.
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

        services.AddHttpClient(HttpClientName, client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
        });

        services.AddSingleton<IMessageBrokerProducer, NorthguardProducer>();
        services.AddSingleton<IMessageBrokerConsumer, NorthguardConsumer>();

        return services;
    }
}
