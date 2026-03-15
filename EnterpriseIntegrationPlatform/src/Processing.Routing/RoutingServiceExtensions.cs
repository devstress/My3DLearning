using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Extension methods for registering the Content-Based Router in the DI container.
/// </summary>
public static class RoutingServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IContentBasedRouter"/> and its dependencies, binding
    /// <see cref="RouterOptions"/> from the <c>ContentBasedRouter</c> configuration section.
    /// </summary>
    /// <remarks>
    /// An <see cref="Ingestion.IMessageBrokerProducer"/> must be registered separately
    /// (e.g. via <c>AddNatsJetStreamBroker</c>) before calling this method.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContentBasedRouter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RouterOptions>(
            configuration.GetSection(RouterOptions.SectionName));

        services.AddSingleton<IContentBasedRouter, ContentBasedRouter>();

        return services;
    }
}
