using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to NATS JetStream subjects.
/// Each subject acts as an independent channel, avoiding Head-of-Line blocking
/// between recipients.
/// </summary>
public sealed class NatsJetStreamProducer : IMessageBrokerProducer
{
    private readonly INatsConnection _connection;
    private readonly INatsJSContext _js;
    private readonly ILogger<NatsJetStreamProducer> _logger;

    /// <summary>Initialises a new <see cref="NatsJetStreamProducer"/>.</summary>
    public NatsJetStreamProducer(INatsConnection connection, ILogger<NatsJetStreamProducer> logger)
    {
        _connection = connection;
        _js = new NatsJSContext((NatsConnection)connection);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var data = EnvelopeSerializer.Serialize(envelope);

        await EnsureStreamAsync(topic, cancellationToken);

        await _js.PublishAsync(
            subject: topic,
            data: data,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published message {MessageId} to NATS subject {Subject}",
            envelope.MessageId, topic);
    }

    private async Task EnsureStreamAsync(string topic, CancellationToken ct)
    {
        var streamName = topic.Replace(".", "-");

        // Retry transient JetStream API timeouts (e.g. during container startup
        // or under heavy concurrent stream creation load in CI).
        const int maxRetries = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _js.GetStreamAsync(streamName, cancellationToken: ct);
                return; // Stream exists
            }
            catch (NatsJSApiException ex) when (ex.Error.Code == 404)
            {
                // Stream does not exist — create it (may also need retry)
                try
                {
                    await _js.CreateStreamAsync(
                        new StreamConfig(streamName, [topic]),
                        ct);
                    return;
                }
                catch (NatsJSApiNoResponseException) when (attempt < maxRetries)
                {
                    _logger.LogWarning(
                        "JetStream create-stream timeout (attempt {Attempt}/{Max}), retrying…",
                        attempt, maxRetries);
                    await Task.Delay(1_000 * attempt, ct);
                }
            }
            catch (NatsJSApiNoResponseException) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "JetStream get-stream timeout (attempt {Attempt}/{Max}), retrying…",
                    attempt, maxRetries);
                await Task.Delay(1_000 * attempt, ct);
            }
        }
    }
}
