using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Messaging Bridge implementation — forwards messages from a source broker to a target
/// broker with envelope preservation and sliding-window deduplication by MessageId.
/// </summary>
public sealed class MessagingBridge : IMessagingBridge
{
    private readonly IMessageBrokerConsumer _sourceConsumer;
    private readonly IMessageBrokerProducer _targetProducer;
    private readonly MessagingBridgeOptions _options;
    private readonly ILogger<MessagingBridge> _logger;

    private readonly ConcurrentDictionary<Guid, byte> _seenIds = new();
    private readonly ConcurrentQueue<Guid> _evictionQueue = new();
    private long _forwardedCount;
    private long _duplicateCount;

    /// <summary>
    /// Initializes a new instance of <see cref="MessagingBridge"/>.
    /// </summary>
    /// <param name="sourceConsumer">Consumer connected to the source broker.</param>
    /// <param name="targetProducer">Producer connected to the target broker.</param>
    /// <param name="options">Bridge configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public MessagingBridge(
        IMessageBrokerConsumer sourceConsumer,
        IMessageBrokerProducer targetProducer,
        IOptions<MessagingBridgeOptions> options,
        ILogger<MessagingBridge> logger)
    {
        _sourceConsumer = sourceConsumer ?? throw new ArgumentNullException(nameof(sourceConsumer));
        _targetProducer = targetProducer ?? throw new ArgumentNullException(nameof(targetProducer));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public long ForwardedCount => Interlocked.Read(ref _forwardedCount);

    /// <inheritdoc />
    public long DuplicateCount => Interlocked.Read(ref _duplicateCount);

    /// <inheritdoc />
    public async Task StartAsync<T>(
        string sourceChannel,
        string targetChannel,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceChannel);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetChannel);

        _logger.LogInformation(
            "Messaging bridge starting: {SourceChannel} → {TargetChannel}, ConsumerGroup={ConsumerGroup}, DeduplicationWindow={Window}",
            sourceChannel, targetChannel, _options.ConsumerGroup, _options.DeduplicationWindowSize);

        await _sourceConsumer.SubscribeAsync<T>(
            sourceChannel,
            _options.ConsumerGroup,
            async envelope => await ForwardAsync(envelope, targetChannel, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Forwards a single message to the target broker after deduplication check.
    /// </summary>
    private async Task ForwardAsync<T>(
        IntegrationEnvelope<T> envelope,
        string targetChannel,
        CancellationToken cancellationToken)
    {
        if (!TryTrackMessageId(envelope.MessageId))
        {
            Interlocked.Increment(ref _duplicateCount);

            _logger.LogDebug(
                "Bridge duplicate skipped: MessageId={MessageId}", envelope.MessageId);

            return;
        }

        await _targetProducer.PublishAsync(envelope, targetChannel, cancellationToken);

        Interlocked.Increment(ref _forwardedCount);

        _logger.LogDebug(
            "Bridge forwarded: MessageId={MessageId}, Target={TargetChannel}",
            envelope.MessageId, targetChannel);
    }

    /// <summary>
    /// Tracks a message ID in the sliding deduplication window.
    /// Returns <c>true</c> if the ID is new; <c>false</c> if it was already seen.
    /// </summary>
    private bool TryTrackMessageId(Guid messageId)
    {
        if (!_seenIds.TryAdd(messageId, 0))
        {
            return false;
        }

        _evictionQueue.Enqueue(messageId);

        // Evict oldest IDs when the window is exceeded.
        while (_seenIds.Count > _options.DeduplicationWindowSize
            && _evictionQueue.TryDequeue(out var oldId))
        {
            _seenIds.TryRemove(oldId, out _);
        }

        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sourceConsumer.DisposeAsync();

        _logger.LogInformation(
            "Messaging bridge disposed. Forwarded={Forwarded}, Duplicates={Duplicates}",
            ForwardedCount, DuplicateCount);
    }
}
