using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Implements the Aggregator Enterprise Integration Pattern.
/// Collects individual <see cref="IntegrationEnvelope{TItem}"/> messages belonging
/// to the same correlation group and, once the group is complete, combines them into
/// a single <see cref="IntegrationEnvelope{TAggregate}"/> and publishes it to the
/// configured target topic.
/// </summary>
/// <typeparam name="TItem">The payload type of the individual messages.</typeparam>
/// <typeparam name="TAggregate">The payload type of the aggregated message.</typeparam>
public interface IMessageAggregator<TItem, TAggregate>
{
    /// <summary>
    /// Adds <paramref name="envelope"/> to its correlation group.
    /// If the group becomes complete, the group is aggregated, the aggregate envelope
    /// is published, and an <see cref="AggregateResult{TAggregate}"/> with
    /// <see cref="AggregateResult{TAggregate}.IsComplete"/> set to <see langword="true"/>
    /// is returned. Otherwise the result indicates the group is still accumulating.
    /// </summary>
    /// <param name="envelope">The individual envelope to add to its correlation group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AggregateResult{TAggregate}"/> describing the current state of
    /// the correlation group after this envelope was added.
    /// </returns>
    Task<AggregateResult<TAggregate>> AggregateAsync(
        IntegrationEnvelope<TItem> envelope,
        CancellationToken cancellationToken = default);
}
