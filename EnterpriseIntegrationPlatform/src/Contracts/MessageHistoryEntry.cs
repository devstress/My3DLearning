namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Represents a single step in a message's processing history.
/// Used by the Message History Enterprise Integration Pattern to track
/// the chain of processing activities a message has traversed.
/// </summary>
/// <param name="ActivityName">The name of the processing step or activity.</param>
/// <param name="Timestamp">The UTC timestamp when this step was recorded.</param>
/// <param name="Status">The outcome of the processing step.</param>
/// <param name="Detail">Optional detail about the step (e.g. routing decision, error message).</param>
public sealed record MessageHistoryEntry(
    string ActivityName,
    DateTimeOffset Timestamp,
    MessageHistoryStatus Status,
    string? Detail = null);

/// <summary>
/// The outcome of a processing step in the message history chain.
/// </summary>
public enum MessageHistoryStatus
{
    /// <summary>The step completed successfully.</summary>
    Completed = 0,

    /// <summary>The step was skipped (e.g. by a filter or detour).</summary>
    Skipped = 1,

    /// <summary>The step failed and the message was routed to an error handler.</summary>
    Failed = 2,

    /// <summary>The step is currently in progress.</summary>
    InProgress = 3
}
