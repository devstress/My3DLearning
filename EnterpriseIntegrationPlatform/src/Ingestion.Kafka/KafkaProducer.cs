using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Kafka;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to Apache Kafka topics.
/// Kafka is used for broadcast event streams, audit logs, fan-out analytics,
/// and decoupled integration.
/// </summary>
public sealed class KafkaProducer : IMessageBrokerProducer, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    /// <summary>Initialises a new <see cref="KafkaProducer"/>.</summary>
    public KafkaProducer(IProducer<string, byte[]> producer, ILogger<KafkaProducer> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(logger);
        _producer = producer;
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
        var message = new Message<string, byte[]>
        {
            Key = envelope.CorrelationId.ToString(),
            Value = data,
        };

        await _producer.ProduceAsync(topic, message, cancellationToken);

        _logger.LogDebug(
            "Published message {MessageId} to Kafka topic {Topic}",
            envelope.MessageId, topic);
    }

    /// <inheritdoc />
    public void Dispose() => _producer.Dispose();
}
