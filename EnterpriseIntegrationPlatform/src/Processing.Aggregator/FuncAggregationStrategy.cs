namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// An <see cref="IAggregationStrategy{TItem,TAggregate}"/> backed by a caller-supplied
/// delegate. Use for inline or lambda-based aggregation logic.
/// </summary>
/// <typeparam name="TItem">The payload type of the individual messages.</typeparam>
/// <typeparam name="TAggregate">The payload type of the aggregated message.</typeparam>
public sealed class FuncAggregationStrategy<TItem, TAggregate> : IAggregationStrategy<TItem, TAggregate>
{
    private readonly Func<IReadOnlyList<TItem>, TAggregate> _aggregateFunc;

    /// <summary>
    /// Initialises a new instance of <see cref="FuncAggregationStrategy{TItem,TAggregate}"/>.
    /// </summary>
    /// <param name="aggregateFunc">Delegate that combines the items into an aggregate.</param>
    public FuncAggregationStrategy(Func<IReadOnlyList<TItem>, TAggregate> aggregateFunc)
    {
        ArgumentNullException.ThrowIfNull(aggregateFunc);
        _aggregateFunc = aggregateFunc;
    }

    /// <inheritdoc />
    public TAggregate Aggregate(IReadOnlyList<TItem> items) => _aggregateFunc(items);
}
