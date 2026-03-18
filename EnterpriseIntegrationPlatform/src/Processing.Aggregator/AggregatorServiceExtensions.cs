using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Extension methods for registering Message Aggregator services into the DI container.
/// </summary>
public static class AggregatorServiceExtensions
{
    /// <summary>
    /// Registers a <see cref="MessageAggregator{TItem,TAggregate}"/> backed by delegate-based
    /// aggregation and count-based completion strategies.
    /// </summary>
    /// <typeparam name="TItem">The payload type of the individual messages.</typeparam>
    /// <typeparam name="TAggregate">The payload type of the aggregated message.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>MessageAggregator</c> section is bound to
    /// <see cref="AggregatorOptions"/>. <see cref="AggregatorOptions.ExpectedCount"/> must be
    /// greater than zero when no custom <paramref name="completionPredicate"/> is provided.
    /// </param>
    /// <param name="aggregateFunc">
    /// Delegate that combines individual payloads into an aggregate.
    /// </param>
    /// <param name="completionPredicate">
    /// Optional custom completion predicate. When <see langword="null"/>, a
    /// <see cref="CountCompletionStrategy{TItem}"/> using
    /// <see cref="AggregatorOptions.ExpectedCount"/> is used.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddMessageAggregator<TItem, TAggregate>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IReadOnlyList<TItem>, TAggregate> aggregateFunc,
        Func<IReadOnlyList<Contracts.IntegrationEnvelope<TItem>>, bool>? completionPredicate = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(aggregateFunc);

        services.Configure<AggregatorOptions>(configuration.GetSection("MessageAggregator"));

        services.AddSingleton<IAggregationStrategy<TItem, TAggregate>>(
            new FuncAggregationStrategy<TItem, TAggregate>(aggregateFunc));

        if (completionPredicate is not null)
        {
            services.AddSingleton<ICompletionStrategy<TItem>>(
                new FuncCompletionStrategy<TItem>(completionPredicate));
        }
        else
        {
            services.AddSingleton<ICompletionStrategy<TItem>>(sp =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AggregatorOptions>>().Value;
                return new CountCompletionStrategy<TItem>(opts.ExpectedCount);
            });
        }

        services.AddSingleton<IMessageAggregateStore<TItem>, InMemoryMessageAggregateStore<TItem>>();
        services.AddSingleton<IMessageAggregator<TItem, TAggregate>, MessageAggregator<TItem, TAggregate>>();

        return services;
    }
}
