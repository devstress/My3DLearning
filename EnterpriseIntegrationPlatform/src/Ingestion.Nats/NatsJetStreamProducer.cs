using System.Text.Json;
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
        try
        {
            await _js.GetStreamAsync(streamName, cancellationToken: ct);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            await _js.CreateStreamAsync(
                new StreamConfig(streamName, [topic]),
                ct);
        }
    }
}
