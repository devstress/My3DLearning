using Temporalio.Activities;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

/// <summary>
/// Temporal activity definitions for the full integration pipeline.
/// Each activity wraps a single side-effect (persistence, notification) so that
/// the <see cref="Workflows.IntegrationPipelineWorkflow"/> can orchestrate them
/// atomically with Temporal's durability guarantees.
/// <para>
/// If any activity fails, Temporal retries it according to the configured retry
/// policy. If the workflow is interrupted (process crash, restart), Temporal
/// resumes from the last completed activity — no work is lost and no partial
/// state is left behind.
/// </para>
/// </summary>
public sealed class PipelineActivities
{
    private readonly IPersistenceActivityService _persistence;
    private readonly INotificationActivityService _notification;
    private readonly IMessageLoggingService _logging;

    public PipelineActivities(
        IPersistenceActivityService persistence,
        INotificationActivityService notification,
        IMessageLoggingService logging)
    {
        _persistence = persistence;
        _notification = notification;
        _logging = logging;
    }

    /// <summary>
    /// Persists the inbound message to Cassandra as Pending.
    /// </summary>
    [Activity]
    public async Task PersistMessageAsync(IntegrationPipelineInput input)
    {
        await _persistence.SaveMessageAsync(input);
    }

    /// <summary>
    /// Updates the delivery status of a message in Cassandra.
    /// </summary>
    [Activity]
    public async Task UpdateDeliveryStatusAsync(
        Guid messageId, Guid correlationId, DateTimeOffset recordedAt, string status)
    {
        await _persistence.UpdateDeliveryStatusAsync(
            messageId, correlationId, recordedAt, status);
    }

    /// <summary>
    /// Saves a fault envelope for a message that could not be processed.
    /// </summary>
    [Activity]
    public async Task SaveFaultAsync(
        Guid messageId, Guid correlationId, string messageType,
        string faultedBy, string reason, int retryCount)
    {
        await _persistence.SaveFaultAsync(
            messageId, correlationId, messageType, faultedBy, reason, retryCount);
    }

    /// <summary>
    /// Publishes an Ack notification to the message broker.
    /// </summary>
    [Activity]
    public async Task PublishAckAsync(Guid messageId, Guid correlationId, string topic)
    {
        await _notification.PublishAckAsync(messageId, correlationId, topic);
    }

    /// <summary>
    /// Publishes a Nack notification to the message broker.
    /// </summary>
    [Activity]
    public async Task PublishNackAsync(
        Guid messageId, Guid correlationId, string reason, string topic)
    {
        await _notification.PublishNackAsync(messageId, correlationId, reason, topic);
    }

    /// <summary>
    /// Logs a processing stage for observability / lifecycle tracking.
    /// </summary>
    [Activity]
    public async Task LogStageAsync(Guid messageId, string messageType, string stage)
    {
        await _logging.LogAsync(messageId, messageType, stage);
    }
}
