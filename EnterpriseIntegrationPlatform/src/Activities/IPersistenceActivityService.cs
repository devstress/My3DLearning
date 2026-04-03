namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Service interface for persistence-related Temporal activities.
/// Implementations interact with the durable store (e.g. Cassandra) to
/// persist messages, update delivery status, and record fault envelopes.
/// </summary>
public interface IPersistenceActivityService
{
    /// <summary>
    /// Persists the inbound message as <c>Pending</c>.
    /// </summary>
    Task SaveMessageAsync(IntegrationPipelineInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the delivery status of a previously persisted message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="correlationId">The correlation identifier (partition key).</param>
    /// <param name="recordedAt">Original recorded-at timestamp (clustering key).</param>
    /// <param name="status">The new delivery status string ("Delivered" or "Failed").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDeliveryStatusAsync(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset recordedAt,
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a fault envelope for a message that could not be processed.
    /// </summary>
    /// <param name="messageId">The faulted message identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="messageType">The original message type.</param>
    /// <param name="faultedBy">Service that generated the fault.</param>
    /// <param name="reason">Human-readable failure reason.</param>
    /// <param name="retryCount">Number of retries attempted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveFaultAsync(
        Guid messageId,
        Guid correlationId,
        string messageType,
        string faultedBy,
        string reason,
        int retryCount,
        CancellationToken cancellationToken = default);
}
