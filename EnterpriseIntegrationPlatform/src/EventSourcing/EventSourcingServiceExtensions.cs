using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Extension methods for registering Event Sourcing services into the DI container.
/// </summary>
public static class EventSourcingServiceExtensions
{
    /// <summary>
    /// Registers the core event-sourcing infrastructure: <see cref="IEventStore"/>,
    /// <see cref="ISnapshotStore{TState}"/>, <see cref="IEventProjection{TState}"/>,
    /// and <see cref="EventProjectionEngine{TState}"/>.
    /// Options are bound from the <c>EventSourcing</c> configuration section.
    /// </summary>
    /// <typeparam name="TState">The type of the projected state.</typeparam>
    /// <typeparam name="TProjection">
    /// Concrete <see cref="IEventProjection{TState}"/> implementation to register.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddEventSourcing<TState, TProjection>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TState : notnull
        where TProjection : class, IEventProjection<TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EventSourcingOptions>(configuration.GetSection("EventSourcing"));

        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<ISnapshotStore<TState>, InMemorySnapshotStore<TState>>();
        services.AddSingleton<IEventProjection<TState>, TProjection>();
        services.AddSingleton<EventProjectionEngine<TState>>();

        return services;
    }
}
