using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// Production implementation of the Splitter Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The splitter delegates payload decomposition to an injected
/// <see cref="ISplitStrategy{T}"/>, then wraps each resulting item in a new
/// <see cref="IntegrationEnvelope{T}"/> that preserves the source envelope's
/// <c>CorrelationId</c>, <c>Priority</c>, <c>SchemaVersion</c>, and <c>Metadata</c>.
/// The <c>CausationId</c> of every split envelope is set to the source
/// <see cref="IntegrationEnvelope{T}.MessageId"/> to maintain the full causation chain.
/// </para>
/// <para>
/// All split envelopes are published to <see cref="SplitterOptions.TargetTopic"/>
/// via the registered <see cref="IMessageBrokerProducer"/>. If any publish fails, the
/// exception propagates immediately — callers should rely on the messaging infrastructure's
/// retry/compensation mechanisms for atomicity.
/// </para>
/// </remarks>
/// <typeparam name="T">The payload type of the envelope.</typeparam>
public sealed class MessageSplitter<T> : IMessageSplitter<T>
{
    private readonly ISplitStrategy<T> _strategy;
    private readonly IMessageBrokerProducer _producer;
    private readonly SplitterOptions _options;
    private readonly ILogger<MessageSplitter<T>> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageSplitter{T}"/>.</summary>
    public MessageSplitter(
        ISplitStrategy<T> strategy,
        IMessageBrokerProducer producer,
        IOptions<SplitterOptions> options,
        ILogger<MessageSplitter<T>> logger)
    {
        _strategy = strategy;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SplitResult<T>> SplitAsync(
        IntegrationEnvelope<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(_options.TargetTopic))
            throw new InvalidOperationException(
                $"MessageSplitter: TargetTopic is not configured. " +
                $"Envelope {source.MessageId} (type='{source.MessageType}') cannot be split.");

        var items = _strategy.Split(source.Payload);

        if (items.Count == 0)
        {
            _logger.LogWarning(
                "Message {MessageId} (type={MessageType}) produced zero items after split — " +
                "no messages published to '{TargetTopic}'",
                source.MessageId, source.MessageType, _options.TargetTopic);

            return new SplitResult<T>([], source.MessageId, _options.TargetTopic, 0);
        }

        var splitEnvelopes = new List<IntegrationEnvelope<T>>(items.Count);

        foreach (var item in items)
        {
            var envelope = new IntegrationEnvelope<T>
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = source.CorrelationId,
                CausationId = source.MessageId,
                Timestamp = DateTimeOffset.UtcNow,
                Source = string.IsNullOrWhiteSpace(_options.TargetSource)
                    ? source.Source
                    : _options.TargetSource,
                MessageType = string.IsNullOrWhiteSpace(_options.TargetMessageType)
                    ? source.MessageType
                    : _options.TargetMessageType,
                SchemaVersion = source.SchemaVersion,
                Priority = source.Priority,
                Payload = item,
                Metadata = new Dictionary<string, string>(source.Metadata),
            };

            await _producer.PublishAsync(envelope, _options.TargetTopic, cancellationToken);
            splitEnvelopes.Add(envelope);
        }

        _logger.LogDebug(
            "Message {SourceMessageId} (type={SourceType}) split into {ItemCount} items " +
            "and published to '{TargetTopic}'",
            source.MessageId,
            source.MessageType,
            splitEnvelopes.Count,
            _options.TargetTopic);

        return new SplitResult<T>(splitEnvelopes, source.MessageId, _options.TargetTopic, splitEnvelopes.Count);
    }
}
