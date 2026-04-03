using Microsoft.Extensions.Logging;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Services;

/// <summary>
/// Notification activity service backed by the configured message broker (NATS JetStream).
/// Used by <see cref="Activities.PipelineActivities"/> to publish Ack/Nack messages
/// as Temporal activities, ensuring notification delivery is durable and retried.
/// </summary>
public sealed class NatsNotificationActivityService : Activities.INotificationActivityService
{
    private const string ServiceName = "Workflow.Temporal";

    private readonly IMessageBrokerProducer _producer;
    private readonly ILogger<NatsNotificationActivityService> _logger;

    public NatsNotificationActivityService(
        IMessageBrokerProducer producer,
        ILogger<NatsNotificationActivityService> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAckAsync(
        Guid messageId,
        Guid correlationId,
        string topic,
        CancellationToken cancellationToken = default)
    {
        var ack = IntegrationEnvelope<AckPayload>.Create(
            new AckPayload(messageId, correlationId, "Delivered"),
            source: ServiceName,
            messageType: "Integration.Ack",
            correlationId: correlationId,
            causationId: messageId);

        await _producer.PublishAsync(ack, topic, cancellationToken);

        _logger.LogDebug(
            "Published Ack for message {MessageId} to {Topic}",
            messageId, topic);
    }

    /// <inheritdoc />
    public async Task PublishNackAsync(
        Guid messageId,
        Guid correlationId,
        string reason,
        string topic,
        CancellationToken cancellationToken = default)
    {
        var nack = IntegrationEnvelope<NackPayload>.Create(
            new NackPayload(messageId, correlationId, reason),
            source: ServiceName,
            messageType: "Integration.Nack",
            correlationId: correlationId,
            causationId: messageId);

        await _producer.PublishAsync(nack, topic, cancellationToken);

        _logger.LogDebug(
            "Published Nack for message {MessageId} to {Topic}: {Reason}",
            messageId, topic, reason);
    }

    /// <summary>
    /// Payload published to the Ack topic when a message is processed successfully.
    /// </summary>
    public record AckPayload(Guid OriginalMessageId, Guid CorrelationId, string Outcome);

    /// <summary>
    /// Payload published to the Nack topic when message processing fails.
    /// </summary>
    public record NackPayload(Guid OriginalMessageId, Guid CorrelationId, string Reason);
}
