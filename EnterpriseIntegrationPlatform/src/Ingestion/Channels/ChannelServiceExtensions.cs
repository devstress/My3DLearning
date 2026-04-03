using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Extension methods for registering Messaging Channel services with the DI container.
/// </summary>
public static class ChannelServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IPointToPointChannel"/> and <see cref="IPublishSubscribeChannel"/>
    /// channel abstractions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessagingChannels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPointToPointChannel, PointToPointChannel>();
        services.AddScoped<IPublishSubscribeChannel, PublishSubscribeChannel>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IDatatypeChannel"/> with configurable topic prefix.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root for binding options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatatypeChannel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatatypeChannelOptions>(configuration.GetSection("DatatypeChannel"));
        services.AddScoped<IDatatypeChannel, DatatypeChannel>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IInvalidMessageChannel"/> for routing malformed messages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root for binding options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInvalidMessageChannel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<InvalidMessageChannelOptions>(configuration.GetSection("InvalidMessageChannel"));
        services.AddScoped<IInvalidMessageChannel, InvalidMessageChannel>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IMessagingBridge"/> for forwarding messages between broker instances.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root for binding options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessagingBridge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MessagingBridgeOptions>(configuration.GetSection("MessagingBridge"));
        services.AddScoped<IMessagingBridge, MessagingBridge>();

        return services;
    }
}
