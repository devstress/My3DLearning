namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Service interface for logging message lifecycle events during workflow execution.
/// </summary>
public interface IMessageLoggingService
{
    /// <summary>
    /// Logs that a message has been received and is being processed.
    /// </summary>
    /// <param name="messageId">Unique message identifier.</param>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="stage">Processing stage name (e.g. "Validated", "Routed").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAsync(Guid messageId, string messageType, string stage);
}

/// <summary>
/// Default message logging service that writes to console/structured logging.
/// </summary>
public sealed class DefaultMessageLoggingService : IMessageLoggingService
{
    /// <inheritdoc />
    public Task LogAsync(Guid messageId, string messageType, string stage)
    {
        // In production this would write to ILogger<T>.
        // Kept simple for the initial chunk; will be enhanced with
        // MessageLifecycleRecorder integration in a future chunk.
        Console.WriteLine(
            "[Workflow] Message {0} ({1}) — stage: {2}",
            messageId,
            messageType,
            stage);

        return Task.CompletedTask;
    }
}
