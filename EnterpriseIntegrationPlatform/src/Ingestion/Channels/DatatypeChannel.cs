using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Datatype Channel implementation — resolves the target topic from
/// <see cref="IntegrationEnvelope{T}.MessageType"/> using a configurable prefix.
/// Each message type gets its own dedicated channel for type-safe processing.
/// </summary>
public sealed class DatatypeChannel : IDatatypeChannel
{
    private readonly IMessageBrokerProducer _producer;
    private readonly DatatypeChannelOptions _options;
    private readonly ILogger<DatatypeChannel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DatatypeChannel"/>.
    /// </summary>
    /// <param name="producer">The message broker producer.</param>
    /// <param name="options">Datatype channel configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public DatatypeChannel(
        IMessageBrokerProducer producer,
        IOptions<DatatypeChannelOptions> options,
        ILogger<DatatypeChannel> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            throw new InvalidOperationException(
                $"Cannot route envelope {envelope.MessageId}: MessageType is null or empty.");
        }

        var channel = ResolveChannel(envelope.MessageType);

        _logger.LogDebug(
            "Datatype channel publish: MessageType={MessageType}, Channel={Channel}, MessageId={MessageId}",
            envelope.MessageType, channel, envelope.MessageId);

        await _producer.PublishAsync(envelope, channel, cancellationToken);
    }

    /// <inheritdoc />
    public string ResolveChannel(string messageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        var prefix = string.IsNullOrWhiteSpace(_options.TopicPrefix) ? string.Empty : _options.TopicPrefix;
        var separator = _options.Separator ?? ".";

        return string.IsNullOrEmpty(prefix)
            ? messageType.ToLowerInvariant()
            : $"{prefix}{separator}{messageType.ToLowerInvariant()}";
    }
}
