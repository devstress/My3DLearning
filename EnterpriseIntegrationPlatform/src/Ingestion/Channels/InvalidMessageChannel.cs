using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Invalid Message Channel implementation — routes unparseable or invalid-schema messages
/// to a dedicated invalid-message topic. Distinct from DLQ: DLQ handles processing failures
/// on well-formed messages; this channel handles malformed input.
/// </summary>
public sealed class InvalidMessageChannel : IInvalidMessageChannel
{
    private readonly IMessageBrokerProducer _producer;
    private readonly InvalidMessageChannelOptions _options;
    private readonly ILogger<InvalidMessageChannel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidMessageChannel"/>.
    /// </summary>
    /// <param name="producer">The message broker producer.</param>
    /// <param name="options">Invalid message channel configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public InvalidMessageChannel(
        IMessageBrokerProducer producer,
        IOptions<InvalidMessageChannelOptions> options,
        ILogger<InvalidMessageChannel> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task RouteInvalidAsync<T>(
        IntegrationEnvelope<T> envelope,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        ValidateTopic();

        string rawData;
        try
        {
            rawData = JsonSerializer.Serialize(envelope.Payload);
        }
        catch (Exception)
        {
            rawData = envelope.Payload?.ToString() ?? "<null>";
        }

        var sourceTopic = envelope.Metadata.TryGetValue(MessageHeaders.SourceTopic, out var st) ? st : "unknown";

        var invalidEnvelope = new InvalidMessageEnvelope
        {
            OriginalMessageId = envelope.MessageId,
            RawData = rawData,
            SourceTopic = sourceTopic,
            Reason = reason,
            RejectedAt = DateTimeOffset.UtcNow,
        };

        var wrappedEnvelope = IntegrationEnvelope<InvalidMessageEnvelope>.Create(
            invalidEnvelope,
            _options.Source,
            "InvalidMessage",
            correlationId: envelope.CorrelationId,
            causationId: envelope.MessageId);

        _logger.LogWarning(
            "Invalid message routed: MessageId={MessageId}, Reason={Reason}, Target={Topic}",
            envelope.MessageId, reason, _options.InvalidMessageTopic);

        await _producer.PublishAsync(wrappedEnvelope, _options.InvalidMessageTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RouteRawInvalidAsync(
        string rawData,
        string sourceTopic,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawData);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTopic);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        ValidateTopic();

        var invalidEnvelope = new InvalidMessageEnvelope
        {
            OriginalMessageId = Guid.Empty,
            RawData = rawData,
            SourceTopic = sourceTopic,
            Reason = reason,
            RejectedAt = DateTimeOffset.UtcNow,
        };

        var wrappedEnvelope = IntegrationEnvelope<InvalidMessageEnvelope>.Create(
            invalidEnvelope,
            _options.Source,
            "InvalidMessage");

        _logger.LogWarning(
            "Raw invalid data routed: SourceTopic={SourceTopic}, Reason={Reason}, Target={Topic}",
            sourceTopic, reason, _options.InvalidMessageTopic);

        await _producer.PublishAsync(wrappedEnvelope, _options.InvalidMessageTopic, cancellationToken);
    }

    private void ValidateTopic()
    {
        if (string.IsNullOrWhiteSpace(_options.InvalidMessageTopic))
        {
            throw new InvalidOperationException("InvalidMessageTopic must not be null or whitespace.");
        }
    }
}
