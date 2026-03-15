using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Base class for platform activities. Provides a consistent activity name
/// and structured logging entry point for all derived activities.
/// </summary>
public abstract class BaseActivity
{
    /// <summary>Activity name used in logs and tracing.</summary>
    public abstract string Name { get; }

    /// <summary>
    /// Logs that this activity has started processing.
    /// </summary>
    protected void LogStart(ILogger logger, Guid messageId) =>
        logger.LogInformation("Activity {Activity} started for message {MessageId}", Name, messageId);

    /// <summary>
    /// Logs that this activity has completed processing.
    /// </summary>
    protected void LogComplete(ILogger logger, Guid messageId) =>
        logger.LogInformation("Activity {Activity} completed for message {MessageId}", Name, messageId);
}
