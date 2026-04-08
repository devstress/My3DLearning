using System.Diagnostics;
using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Kafka;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from Apache Kafka topics.
/// Within a consumer group, each partition is consumed by exactly one consumer.
/// Kafka provides strong ordering per partition.
/// </summary>
public sealed class KafkaConsumer : IMessageBrokerConsumer
{
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Kafka.Consumer");

    private readonly ConsumerConfig _config;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

    /// <summary>Initialises a new <see cref="KafkaConsumer"/>.</summary>
    public KafkaConsumer(ConsumerConfig config, ILogger<KafkaConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        var config = new ConsumerConfig(_config)
        {
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topic);

        _logger.LogInformation(
            "Subscribed to Kafka topic {Topic} with consumer group {Group}",
            topic, consumerGroup);

        while (!linked.Token.IsCancellationRequested)
        {
            using var activity = ActivitySource.StartActivity("Kafka.Consume");
            activity?.SetTag("messaging.system", "kafka");
            activity?.SetTag("messaging.destination", topic);

            try
            {
                var result = consumer.Consume(linked.Token);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                var envelope = EnvelopeSerializer.Deserialize<T>(result.Message.Value);
                if (envelope is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise message on Kafka topic {Topic}", topic);
                    consumer.Commit(result);
                    continue;
                }

                activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

                await handler(envelope);
                consumer.Commit(result);

                _logger.LogDebug(
                    "Processed message {MessageId} from Kafka topic {Topic}",
                    envelope.MessageId, topic);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex,
                    "Error consuming from Kafka topic {Topic}", topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error processing message from Kafka topic {Topic}", topic);
            }
        }

        consumer.Close();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _disposeCts.CancelAsync();
        _disposeCts.Dispose();
    }
}
