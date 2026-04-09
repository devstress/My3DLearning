using System.Diagnostics;
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
public sealed class KafkaProducer : IMessageBrokerProducer, IAsyncDisposable, IDisposable
{
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Kafka.Producer");

    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private bool _disposed;

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
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        using var activity = ActivitySource.StartActivity("Kafka.Publish");
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

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
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error flushing Kafka producer during disposal");
        }

        _producer.Dispose();
    }
}
