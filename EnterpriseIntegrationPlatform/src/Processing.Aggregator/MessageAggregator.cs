using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Production implementation of the Aggregator Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The aggregator stores each incoming <see cref="IntegrationEnvelope{TItem}"/> in the
/// injected <see cref="IMessageAggregateStore{TItem}"/>, keyed by
/// <see cref="IntegrationEnvelope{TItem}.CorrelationId"/>.
/// After every addition the injected <see cref="ICompletionStrategy{TItem}"/> is evaluated.
/// When the group is complete, the injected <see cref="IAggregationStrategy{TItem,TAggregate}"/>
/// combines the individual payloads into a single aggregate payload, a new
/// <see cref="IntegrationEnvelope{TAggregate}"/> is created and published to
/// <see cref="AggregatorOptions.TargetTopic"/> via <see cref="IMessageBrokerProducer"/>,
/// and the group is removed from the store.
/// </para>
/// <para>
/// The aggregate envelope preserves the group's <c>CorrelationId</c> and adopts the
/// highest <see cref="MessagePriority"/> found in the group. The merged <c>Metadata</c>
/// is the union of all individual envelopes' metadata, with later arrivals overriding
/// earlier ones on key conflicts. <c>CausationId</c> is not set on the aggregate envelope
/// because the aggregate has multiple causal messages.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The payload type of the individual messages.</typeparam>
/// <typeparam name="TAggregate">The payload type of the aggregated message.</typeparam>
public sealed class MessageAggregator<TItem, TAggregate> : IMessageAggregator<TItem, TAggregate>
{
    private readonly IMessageAggregateStore<TItem> _store;
    private readonly ICompletionStrategy<TItem> _completionStrategy;
    private readonly IAggregationStrategy<TItem, TAggregate> _aggregationStrategy;
    private readonly IMessageBrokerProducer _producer;
    private readonly AggregatorOptions _options;
    private readonly ILogger<MessageAggregator<TItem, TAggregate>> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageAggregator{TItem,TAggregate}"/>.</summary>
    public MessageAggregator(
        IMessageAggregateStore<TItem> store,
        ICompletionStrategy<TItem> completionStrategy,
        IAggregationStrategy<TItem, TAggregate> aggregationStrategy,
        IMessageBrokerProducer producer,
        IOptions<AggregatorOptions> options,
        ILogger<MessageAggregator<TItem, TAggregate>> logger)
    {
        _store = store;
        _completionStrategy = completionStrategy;
        _aggregationStrategy = aggregationStrategy;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AggregateResult<TAggregate>> AggregateAsync(
        IntegrationEnvelope<TItem> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(_options.TargetTopic))
            throw new InvalidOperationException(
                $"MessageAggregator: TargetTopic is not configured. " +
                $"Envelope {envelope.MessageId} (type='{envelope.MessageType}') cannot be aggregated.");

        var group = await _store.AddAsync(envelope, cancellationToken);

        _logger.LogDebug(
            "Envelope {MessageId} (type={MessageType}, correlationId={CorrelationId}) " +
            "added to aggregation group — group now has {GroupCount} item(s)",
            envelope.MessageId, envelope.MessageType, envelope.CorrelationId, group.Count);

        if (!_completionStrategy.IsComplete(group))
        {
            return new AggregateResult<TAggregate>(
                IsComplete: false,
                AggregateEnvelope: null,
                CorrelationId: envelope.CorrelationId,
                ReceivedCount: group.Count);
        }

        var payloads = group.Select(e => e.Payload).ToList();
        var aggregatePayload = _aggregationStrategy.Aggregate(payloads);

        var mergedMetadata = new Dictionary<string, string>();
        foreach (var e in group)
            foreach (var kv in e.Metadata)
                mergedMetadata[kv.Key] = kv.Value;

        var highestPriority = group.Max(e => e.Priority);
        var first = group[0];

        var aggregateEnvelope = new IntegrationEnvelope<TAggregate>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = envelope.CorrelationId,
            CausationId = null,
            Timestamp = DateTimeOffset.UtcNow,
            Source = string.IsNullOrWhiteSpace(_options.TargetSource)
                ? first.Source
                : _options.TargetSource,
            MessageType = string.IsNullOrWhiteSpace(_options.TargetMessageType)
                ? first.MessageType
                : _options.TargetMessageType,
            SchemaVersion = first.SchemaVersion,
            Priority = highestPriority,
            Payload = aggregatePayload,
            Metadata = mergedMetadata,
        };

        await _store.RemoveGroupAsync(envelope.CorrelationId, cancellationToken);
        await _producer.PublishAsync(aggregateEnvelope, _options.TargetTopic, cancellationToken);

        _logger.LogDebug(
            "Correlation group {CorrelationId} aggregated {ItemCount} item(s) " +
            "and published to '{TargetTopic}' (aggregateId={AggregateMessageId})",
            envelope.CorrelationId, group.Count, _options.TargetTopic, aggregateEnvelope.MessageId);

        return new AggregateResult<TAggregate>(
            IsComplete: true,
            AggregateEnvelope: aggregateEnvelope,
            CorrelationId: envelope.CorrelationId,
            ReceivedCount: group.Count);
    }
}
