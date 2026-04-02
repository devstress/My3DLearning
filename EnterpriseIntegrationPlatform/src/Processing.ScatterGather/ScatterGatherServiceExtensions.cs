using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Extension methods for registering Scatter-Gather services into the DI container.
/// </summary>
public static class ScatterGatherServiceExtensions
{
    /// <summary>
    /// Registers a <see cref="ScatterGatherer{TRequest,TResponse}"/> and its dependencies.
    /// </summary>
    /// <typeparam name="TRequest">The type of the scatter request payload.</typeparam>
    /// <typeparam name="TResponse">The type of the gather response payload.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>ScatterGather</c> section is bound to
    /// <see cref="ScatterGatherOptions"/>.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddScatterGather<TRequest, TResponse>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ScatterGatherOptions>(configuration.GetSection("ScatterGather"));
        services.AddSingleton<ScatterGatherer<TRequest, TResponse>>();
        services.AddSingleton<IScatterGatherer<TRequest, TResponse>>(
            sp => sp.GetRequiredService<ScatterGatherer<TRequest, TResponse>>());

        return services;
    }
}
