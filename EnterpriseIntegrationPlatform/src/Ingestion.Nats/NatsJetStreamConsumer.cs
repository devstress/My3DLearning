using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from NATS JetStream.
/// Uses queue groups so that within a consumer group each message is delivered
/// to exactly one consumer — recipient A does not block recipient B.
/// </summary>
public sealed class NatsJetStreamConsumer : IMessageBrokerConsumer
{
    private readonly INatsConnection _connection;
    private readonly INatsJSContext _js;
    private readonly ILogger<NatsJetStreamConsumer> _logger;

    /// <summary>Initialises a new <see cref="NatsJetStreamConsumer"/>.</summary>
    public NatsJetStreamConsumer(INatsConnection connection, ILogger<NatsJetStreamConsumer> logger)
    {
        _connection = connection;
        _js = new NatsJSContext((NatsConnection)connection);
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

        var streamName = topic.Replace(".", "-");

        await EnsureStreamAsync(streamName, topic, cancellationToken);

        var consumer = await _js.CreateOrUpdateConsumerAsync(
            streamName,
            new ConsumerConfig(consumerGroup)
            {
                FilterSubject = topic,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
            },
            cancellationToken);

        _logger.LogInformation(
            "Subscribed to NATS JetStream subject {Subject} with consumer group {Group}",
            topic, consumerGroup);

        await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
        {
            try
            {
                if (msg.Data is null)
                {
                    _logger.LogWarning("Received null data on subject {Subject}", topic);
                    await msg.AckAsync(cancellationToken: cancellationToken);
                    continue;
                }

                var envelope = EnvelopeSerializer.Deserialize<T>(msg.Data);
                if (envelope is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise message on subject {Subject}", topic);
                    await msg.AckAsync(cancellationToken: cancellationToken);
                    continue;
                }

                await handler(envelope);
                await msg.AckAsync(cancellationToken: cancellationToken);

                _logger.LogDebug(
                    "Processed message {MessageId} from NATS subject {Subject}",
                    envelope.MessageId, topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error processing message from NATS subject {Subject}", topic);
                await msg.NakAsync(cancellationToken: cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task EnsureStreamAsync(string streamName, string topic, CancellationToken ct)
    {
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
