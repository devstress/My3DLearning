using Microsoft.Extensions.Logging;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Services;

/// <summary>
/// Notification activity service backed by the configured message broker (NATS JetStream).
/// Used by <see cref="Temporal.Activities.PipelineActivities"/> to publish Ack/Nack messages
/// as Temporal activities, ensuring notification delivery is durable and retried.
/// <para>
/// Notifications are gated by the <c>"Notifications.Enabled"</c> feature flag.
/// When the flag is disabled (or absent), Ack/Nack messages are silently skipped.
/// This allows operators to turn notifications off and on at runtime without
/// redeploying or reconfiguring individual integrations.
/// </para>
/// </summary>
public sealed class NatsNotificationActivityService : INotificationActivityService
{
    private const string ServiceName = "Workflow.Temporal";

    private readonly IMessageBrokerProducer _producer;
    private readonly IFeatureFlagService _featureFlags;
    private readonly INotificationMapper _mapper;
    private readonly ILogger<NatsNotificationActivityService> _logger;

    public NatsNotificationActivityService(
        IMessageBrokerProducer producer,
        IFeatureFlagService featureFlags,
        INotificationMapper mapper,
        ILogger<NatsNotificationActivityService> logger)
    {
        _producer = producer;
        _featureFlags = featureFlags;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAckAsync(
        Guid messageId,
        Guid correlationId,
        string topic,
        CancellationToken cancellationToken = default)
    {
        if (!await IsNotificationEnabledAsync(cancellationToken))
        {
            _logger.LogDebug(
                "Notifications disabled via feature flag — skipping Ack for message {MessageId}",
                messageId);
            return;
        }

        var mappedPayload = _mapper.MapAck(messageId, correlationId);

        var ack = IntegrationEnvelope<AckPayload>.Create(
            new AckPayload(messageId, correlationId, "Delivered", mappedPayload),
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
        if (!await IsNotificationEnabledAsync(cancellationToken))
        {
            _logger.LogDebug(
                "Notifications disabled via feature flag — skipping Nack for message {MessageId}",
                messageId);
            return;
        }

        var mappedPayload = _mapper.MapNack(messageId, correlationId, reason);

        var nack = IntegrationEnvelope<NackPayload>.Create(
            new NackPayload(messageId, correlationId, reason, mappedPayload),
            source: ServiceName,
            messageType: "Integration.Nack",
            correlationId: correlationId,
            causationId: messageId);

        await _producer.PublishAsync(nack, topic, cancellationToken);

        _logger.LogDebug(
            "Published Nack for message {MessageId} to {Topic}: {Reason}",
            messageId, topic, reason);
    }

    private async Task<bool> IsNotificationEnabledAsync(CancellationToken ct) =>
        await _featureFlags.IsEnabledAsync(
            NotificationFeatureFlags.NotificationsEnabled, tenantId: null, ct);

    /// <summary>
    /// Payload published to the Ack topic when a message is delivered
    /// successfully by a Channel Adapter.
    /// </summary>
    /// <param name="OriginalMessageId">The message that was delivered successfully.</param>
    /// <param name="CorrelationId">Correlation identifier for end-to-end tracing.</param>
    /// <param name="Outcome">Human-readable outcome description.</param>
    /// <param name="MappedResponse">
    /// The formatted notification payload produced by <see cref="INotificationMapper"/>
    /// (e.g. <c>&lt;Ack&gt;ok&lt;/Ack&gt;</c>).
    /// </param>
    public record AckPayload(
        Guid OriginalMessageId,
        Guid CorrelationId,
        string Outcome,
        string MappedResponse);

    /// <summary>
    /// Payload published to the Nack topic when Channel Adapter delivery fails.
    /// </summary>
    /// <param name="OriginalMessageId">The message that failed processing.</param>
    /// <param name="CorrelationId">Correlation identifier for end-to-end tracing.</param>
    /// <param name="Reason">Human-readable failure reason.</param>
    /// <param name="MappedResponse">
    /// The formatted notification payload produced by <see cref="INotificationMapper"/>
    /// (e.g. <c>&lt;Nack&gt;not ok because of timeout&lt;/Nack&gt;</c>).
    /// </param>
    public record NackPayload(
        Guid OriginalMessageId,
        Guid CorrelationId,
        string Reason,
        string MappedResponse);
}
