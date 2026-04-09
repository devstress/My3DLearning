using System.Buffers;
using System.Diagnostics;
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
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Pulsar.Consumer");

    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarConsumer> _logger;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

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
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);

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
            await foreach (var msg in consumer.Messages(linked.Token))
            {
                using var activity = ActivitySource.StartActivity("Pulsar.Consume");
                activity?.SetTag("messaging.system", "pulsar");
                activity?.SetTag("messaging.destination", topic);

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
                        await consumer.Acknowledge(msg, linked.Token);
                        continue;
                    }

                    activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

                    await handler(envelope);
                    await consumer.Acknowledge(msg, linked.Token);

                    _logger.LogDebug(
                        "Processed message {MessageId} from Pulsar topic {Topic}",
                        envelope.MessageId, topic);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "Error processing message from Pulsar topic {Topic}", topic);
                    await consumer.RedeliverUnacknowledgedMessages(
                        [msg.MessageId], linked.Token);
                }
            }
        }
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
