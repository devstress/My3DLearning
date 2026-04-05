using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Replay;

public sealed class MessageReplayer : IMessageReplayer
{
    private readonly IMessageReplayStore _store;
    private readonly IMessageBrokerProducer _producer;
    private readonly ReplayOptions _options;
    private readonly ILogger<MessageReplayer> _logger;

    public MessageReplayer(
        IMessageReplayStore store,
        IMessageBrokerProducer producer,
        IOptions<ReplayOptions> options,
        ILogger<MessageReplayer> logger)
    {
        _store = store;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReplayResult> ReplayAsync(ReplayFilter filter, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.SourceTopic))
            throw new InvalidOperationException("SourceTopic must not be null or whitespace.");
        if (string.IsNullOrWhiteSpace(_options.TargetTopic))
            throw new InvalidOperationException("TargetTopic must not be null or whitespace.");

        var startedAt = DateTimeOffset.UtcNow;
        var replayId = Guid.NewGuid();
        var replayed = 0;
        var skipped = 0;
        var failed = 0;

        await foreach (var envelope in _store.GetMessagesForReplayAsync(
            _options.SourceTopic, filter, _options.MaxMessages, ct))
        {
            // Skip already-replayed messages when dedup is enabled.
            if (_options.SkipAlreadyReplayed &&
                envelope.Metadata.ContainsKey(MessageHeaders.ReplayId))
            {
                skipped++;
                _logger.LogDebug(
                    "Skipped already-replayed message {MessageId}", envelope.MessageId);
                continue;
            }

            try
            {
                var metadata = new Dictionary<string, string>(envelope.Metadata)
                {
                    [MessageHeaders.ReplayId] = replayId.ToString()
                };

                var replayedEnvelope = new IntegrationEnvelope<object>
                {
                    MessageId = Guid.NewGuid(),
                    CorrelationId = envelope.CorrelationId,
                    CausationId = envelope.MessageId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Source = envelope.Source,
                    MessageType = envelope.MessageType,
                    SchemaVersion = envelope.SchemaVersion,
                    Priority = envelope.Priority,
                    Payload = envelope.Payload,
                    Metadata = metadata
                };

                await _producer.PublishAsync(replayedEnvelope, _options.TargetTopic, ct);
                replayed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to replay message {MessageId}", envelope.MessageId);
                failed++;
            }
        }

        return new ReplayResult
        {
            ReplayedCount = replayed,
            SkippedCount = skipped,
            FailedCount = failed,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }
}
