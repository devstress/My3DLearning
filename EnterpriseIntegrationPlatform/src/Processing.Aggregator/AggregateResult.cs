using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// The result returned by <see cref="IMessageAggregator{TItem,TAggregate}.AggregateAsync"/>
/// after processing one individual envelope.
/// </summary>
/// <typeparam name="TAggregate">The payload type of the aggregated envelope.</typeparam>
/// <param name="IsComplete">
/// <see langword="true"/> when the correlation group reached its completion condition
/// and the aggregate envelope was published; <see langword="false"/> when the group is
/// still accumulating messages.
/// </param>
/// <param name="AggregateEnvelope">
/// The published aggregate envelope when <paramref name="IsComplete"/> is
/// <see langword="true"/>; <see langword="null"/> otherwise.
/// </param>
/// <param name="CorrelationId">
/// The correlation identifier shared by all envelopes in the group.
/// </param>
/// <param name="ReceivedCount">
/// The number of individual envelopes received for this correlation group so far,
/// including the envelope just processed.
/// </param>
public sealed record AggregateResult<TAggregate>(
    bool IsComplete,
    IntegrationEnvelope<TAggregate>? AggregateEnvelope,
    Guid CorrelationId,
    int ReceivedCount);
