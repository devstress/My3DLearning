using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Defines a strategy for combining a collection of individual items of type
/// <typeparamref name="TItem"/> into a single aggregate payload of type
/// <typeparamref name="TAggregate"/>.
/// </summary>
/// <typeparam name="TItem">The payload type of the individual messages.</typeparam>
/// <typeparam name="TAggregate">The payload type of the aggregated message.</typeparam>
public interface IAggregationStrategy<TItem, TAggregate>
{
    /// <summary>
    /// Combines the individual <paramref name="items"/> into a single aggregate payload.
    /// </summary>
    /// <param name="items">The ordered list of individual payloads to aggregate.</param>
    /// <returns>The combined aggregate payload.</returns>
    TAggregate Aggregate(IReadOnlyList<TItem> items);
}
