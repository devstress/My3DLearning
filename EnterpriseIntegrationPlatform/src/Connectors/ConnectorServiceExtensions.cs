using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Extension methods for registering the unified connector infrastructure in the DI container.
/// </summary>
public static class ConnectorServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IConnectorRegistry"/> and <see cref="IConnectorFactory"/>
    /// in the DI container as singletons.
    /// </summary>
    /// <remarks>
    /// Individual connectors must be registered afterwards by calling
    /// <see cref="AddConnector{T}"/> or by manually registering <see cref="IConnector"/>
    /// instances into the <see cref="IConnectorRegistry"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnectors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        services.AddSingleton<IConnectorFactory, ConnectorFactory>();

        return services;
    }

    /// <summary>
    /// Registers a concrete <see cref="IConnector"/> implementation as a singleton and
    /// auto-registers it with the <see cref="IConnectorRegistry"/> at first resolution.
    /// </summary>
    /// <typeparam name="T">The concrete connector type implementing <see cref="IConnector"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnector<T>(this IServiceCollection services)
        where T : class, IConnector
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<T>();
        services.AddSingleton<IConnector>(sp =>
        {
            var connector = sp.GetRequiredService<T>();
            var registry = sp.GetRequiredService<IConnectorRegistry>();
            registry.Register(connector);
            return connector;
        });

        return services;
    }

    /// <summary>
    /// Registers a pre-built <see cref="IConnector"/> instance and adds it to the
    /// <see cref="IConnectorRegistry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connector">The connector instance to register.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnectorInstance(
        this IServiceCollection services,
        IConnector connector)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connector);

        services.AddSingleton(connector);
        services.AddSingleton<IConnector>(sp =>
        {
            var registry = sp.GetRequiredService<IConnectorRegistry>();
            registry.Register(connector);
            return connector;
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="ConnectorHealthAggregator"/> as a named health check
    /// that probes all connectors in the <see cref="IConnectorRegistry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">Health check name. Defaults to <c>"connectors"</c>.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails.
    /// Defaults to <see cref="HealthStatus.Unhealthy"/>.
    /// </param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnectorHealthCheck(
        this IServiceCollection services,
        string name = "connectors",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ConnectorHealthAggregator>();
        services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                name,
                sp => sp.GetRequiredService<ConnectorHealthAggregator>(),
                failureStatus,
                tags));

        return services;
    }
}
