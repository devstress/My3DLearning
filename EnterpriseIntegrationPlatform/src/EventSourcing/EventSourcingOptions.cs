namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Configuration options for the Event Sourcing infrastructure.
/// Bind from the <c>EventSourcing</c> configuration section.
/// </summary>
public sealed class EventSourcingOptions
{
    /// <summary>
    /// Number of events applied since the last snapshot before a new snapshot is automatically saved.
    /// Set to <c>0</c> to disable automatic snapshotting. Default is <c>50</c>.
    /// </summary>
    public int SnapshotInterval { get; set; } = 50;

    /// <summary>
    /// Maximum number of events returned in a single read operation.
    /// Limits memory consumption during large stream replays. Default is <c>1000</c>.
    /// </summary>
    public int MaxEventsPerRead { get; set; } = 1000;
}
