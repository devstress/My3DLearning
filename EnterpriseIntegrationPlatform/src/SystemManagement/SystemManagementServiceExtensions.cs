using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// DI registration extensions for System Management EIP patterns.
/// </summary>
public static class SystemManagementServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IControlBus"/> implementation, binding
    /// <see cref="ControlBusOptions"/> from the <c>ControlBus</c> configuration section.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="Ingestion.IMessageBrokerProducer"/> and
    /// <see cref="Ingestion.IMessageBrokerConsumer"/> to be registered.
    /// </remarks>
    public static IServiceCollection AddControlBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ControlBusOptions>(
            configuration.GetSection(ControlBusOptions.SectionName));

        services.AddSingleton<IControlBus, ControlBusPublisher>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IMessageStore"/> implementation.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="Storage.Cassandra.IMessageRepository"/> to be registered.
    /// </remarks>
    public static IServiceCollection AddMessageStore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMessageStore, MessageStore>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="ISmartProxy"/> implementation.
    /// </summary>
    public static IServiceCollection AddSmartProxy(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISmartProxy, SmartProxy>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="ITestMessageGenerator"/> implementation.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="Ingestion.IMessageBrokerProducer"/> to be registered.
    /// </remarks>
    public static IServiceCollection AddTestMessageGenerator(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITestMessageGenerator, TestMessageGenerator>();

        return services;
    }
}
