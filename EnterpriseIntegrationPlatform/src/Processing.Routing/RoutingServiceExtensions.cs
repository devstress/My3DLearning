using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Extension methods for registering routing components in the DI container.
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

    /// <summary>
    /// Registers the <see cref="IDynamicRouter"/> and <see cref="IRouterControlChannel"/>
    /// implementations, binding <see cref="DynamicRouterOptions"/> from the
    /// <c>DynamicRouter</c> configuration section.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both <see cref="IDynamicRouter"/> and <see cref="IRouterControlChannel"/> resolve to
    /// the same <see cref="DynamicRouter"/> singleton instance, ensuring the routing table
    /// is shared between the routing path and the control channel.
    /// </para>
    /// <para>
    /// An <see cref="Ingestion.IMessageBrokerProducer"/> must be registered separately
    /// (e.g. via <c>AddNatsJetStreamBroker</c>) before calling this method.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDynamicRouter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DynamicRouterOptions>(
            configuration.GetSection(DynamicRouterOptions.SectionName));

        services.AddSingleton<DynamicRouter>();
        services.AddSingleton<IDynamicRouter>(sp => sp.GetRequiredService<DynamicRouter>());
        services.AddSingleton<IRouterControlChannel>(sp => sp.GetRequiredService<DynamicRouter>());

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IRecipientList"/> implementation, binding
    /// <see cref="RecipientListOptions"/> from the <c>RecipientList</c> configuration section.
    /// </summary>
    /// <remarks>
    /// An <see cref="Ingestion.IMessageBrokerProducer"/> must be registered separately
    /// (e.g. via <c>AddNatsJetStreamBroker</c>) before calling this method.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRecipientList(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RecipientListOptions>(
            configuration.GetSection(RecipientListOptions.SectionName));

        services.AddSingleton<IRecipientList, RecipientListRouter>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IMessageFilter"/> implementation, binding
    /// <see cref="MessageFilterOptions"/> from the <c>MessageFilter</c> configuration section.
    /// </summary>
    /// <remarks>
    /// An <see cref="Ingestion.IMessageBrokerProducer"/> must be registered separately
    /// (e.g. via <c>AddNatsJetStreamBroker</c>) before calling this method.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageFilter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MessageFilterOptions>(
            configuration.GetSection(MessageFilterOptions.SectionName));

        services.AddSingleton<IMessageFilter, MessageFilter>();

        return services;
    }
}
