using System.Buffers;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from Apache Pulsar topics
/// using <see cref="SubscriptionType.KeyShared"/> subscription. Messages are distributed
/// by key (correlationId) across consumers — all messages for recipient A stay ordered
/// while recipient B is processed by another consumer.
/// </summary>
public sealed class PulsarConsumer : IMessageBrokerConsumer
{
    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarConsumer> _logger;

    /// <summary>Initialises a new <see cref="PulsarConsumer"/>.</summary>
    public PulsarConsumer(IPulsarClient client, ILogger<PulsarConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        var consumer = _client.NewConsumer()
            .SubscriptionName(consumerGroup)
            .Topic(topic)
            .SubscriptionType(SubscriptionType.KeyShared)
            .Create();

        _logger.LogInformation(
            "Subscribed to Pulsar topic {Topic} with Key_Shared subscription {Group}",
            topic, consumerGroup);

        await using (consumer.ConfigureAwait(false))
        {
            await foreach (var msg in consumer.Messages(cancellationToken))
            {
                try
                {
                    var bytes = msg.Data.IsSingleSegment
                        ? msg.Data.FirstSpan
                        : msg.Data.ToArray();
                    var envelope = EnvelopeSerializer.Deserialize<T>(bytes);
                    if (envelope is null)
                    {
                        _logger.LogWarning(
                            "Failed to deserialise message on Pulsar topic {Topic}", topic);
                        await consumer.Acknowledge(msg, cancellationToken);
                        continue;
                    }

                    await handler(envelope);
                    await consumer.Acknowledge(msg, cancellationToken);

                    _logger.LogDebug(
                        "Processed message {MessageId} from Pulsar topic {Topic}",
                        envelope.MessageId, topic);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "Error processing message from Pulsar topic {Topic}", topic);
                    await consumer.RedeliverUnacknowledgedMessages(
                        [msg.MessageId], cancellationToken);
                }
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
