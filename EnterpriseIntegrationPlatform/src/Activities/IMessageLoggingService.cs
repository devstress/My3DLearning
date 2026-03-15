using Microsoft.Extensions.Logging;

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
/// Message logging service that writes structured log entries via <see cref="ILogger"/>.
/// </summary>
public sealed class DefaultMessageLoggingService : IMessageLoggingService
{
    private readonly ILogger<DefaultMessageLoggingService> _logger;

    public DefaultMessageLoggingService(ILogger<DefaultMessageLoggingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task LogAsync(Guid messageId, string messageType, string stage)
    {
        _logger.LogInformation(
            "[Workflow] Message {MessageId} ({MessageType}) — stage: {Stage}",
            messageId,
            messageType,
            stage);

        return Task.CompletedTask;
    }
}
