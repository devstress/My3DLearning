using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to Apache Pulsar topics.
/// Each message is keyed by <see cref="IntegrationEnvelope{T}.CorrelationId"/> so that
/// Key_Shared consumers can distribute messages by recipient without HOL blocking.
/// </summary>
public sealed class PulsarProducer : IMessageBrokerProducer, IAsyncDisposable
{
    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarProducer> _logger;

    /// <summary>Initialises a new <see cref="PulsarProducer"/>.</summary>
    public PulsarProducer(IPulsarClient client, ILogger<PulsarProducer> logger)
    {
        _client = client;
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

        var producer = _client.NewProducer()
            .Topic(topic)
            .Create();

        await using (producer.ConfigureAwait(false))
        {
            var metadata = new MessageMetadata
            {
                Key = envelope.CorrelationId.ToString(),
            };

            await producer.Send(metadata, data, cancellationToken);
        }

        _logger.LogDebug(
            "Published message {MessageId} to Pulsar topic {Topic} with key {Key}",
            envelope.MessageId, topic, envelope.CorrelationId);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
