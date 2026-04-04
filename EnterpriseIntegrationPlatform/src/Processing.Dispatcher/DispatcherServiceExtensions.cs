using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// DI registration extensions for the Message Dispatcher and Service Activator patterns.
/// </summary>
public static class DispatcherServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IMessageDispatcher"/> implementation, binding
    /// <see cref="MessageDispatcherOptions"/> from the <c>MessageDispatcher</c>
    /// configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageDispatcher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MessageDispatcherOptions>(
            configuration.GetSection(MessageDispatcherOptions.SectionName));

        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IServiceActivator"/> implementation, binding
    /// <see cref="ServiceActivatorOptions"/> from the <c>ServiceActivator</c>
    /// configuration section.
    /// </summary>
    /// <remarks>
    /// An <see cref="Ingestion.IMessageBrokerProducer"/> must be registered separately
    /// (e.g. via <c>AddNatsJetStreamBroker</c>) before calling this method.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceActivator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ServiceActivatorOptions>(
            configuration.GetSection(ServiceActivatorOptions.SectionName));

        services.AddSingleton<IServiceActivator, ServiceActivator>();

        return services;
    }
}
