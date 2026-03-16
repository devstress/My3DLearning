using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// Extension methods for registering Message Splitter services into the DI container.
/// </summary>
public static class SplitterServiceExtensions
{
    /// <summary>
    /// Registers a <see cref="MessageSplitter{T}"/> backed by a delegate-based
    /// split strategy.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>MessageSplitter</c> section is bound to
    /// <see cref="SplitterOptions"/>.
    /// </param>
    /// <param name="splitFunc">
    /// Delegate that splits a <typeparamref name="T"/> payload into individual items.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddMessageSplitter<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<T, IReadOnlyList<T>> splitFunc)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(splitFunc);

        services.Configure<SplitterOptions>(configuration.GetSection("MessageSplitter"));
        services.AddSingleton<ISplitStrategy<T>>(new FuncSplitStrategy<T>(splitFunc));
        services.AddSingleton<IMessageSplitter<T>, MessageSplitter<T>>();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MessageSplitter{JsonElement}"/> backed by
    /// <see cref="JsonArraySplitStrategy"/>, which splits a JSON array payload
    /// (or a named array property within a JSON object) into individual
    /// <see cref="JsonElement"/> items.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>MessageSplitter</c> section is bound to
    /// <see cref="SplitterOptions"/>.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddJsonMessageSplitter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SplitterOptions>(configuration.GetSection("MessageSplitter"));
        services.AddSingleton<ISplitStrategy<JsonElement>, JsonArraySplitStrategy>();
        services.AddSingleton<IMessageSplitter<JsonElement>, MessageSplitter<JsonElement>>();
        return services;
    }
}
