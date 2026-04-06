// ============================================================================
// MockAggregationStrategy – Configurable aggregation for testing
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Aggregator;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IAggregationStrategy{TItem,TAggregate}"/>
/// that applies a configurable aggregation function.
/// </summary>
public sealed class MockAggregationStrategy<TItem, TAggregate> : IAggregationStrategy<TItem, TAggregate>
{
    private readonly Func<IReadOnlyList<TItem>, TAggregate> _aggregateFunc;
    private int _callCount;

    /// <summary>Creates a mock strategy with the given aggregation function.</summary>
    public MockAggregationStrategy(Func<IReadOnlyList<TItem>, TAggregate> aggregateFunc) =>
        _aggregateFunc = aggregateFunc;

    /// <summary>Number of aggregation calls.</summary>
    public int CallCount => _callCount;

    public TAggregate Aggregate(IReadOnlyList<TItem> items)
    {
        Interlocked.Increment(ref _callCount);
        return _aggregateFunc(items);
    }
}
