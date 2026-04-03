namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Types of disaster recovery drills that can be executed.
/// </summary>
public enum DrDrillType
{
    /// <summary>Simulate complete region failure — all services become unreachable.</summary>
    RegionFailure,

    /// <summary>Simulate network partition between regions.</summary>
    NetworkPartition,

    /// <summary>Simulate data store unavailability in the target region.</summary>
    StorageFailure,

    /// <summary>Simulate broker/messaging infrastructure failure.</summary>
    BrokerFailure,

    /// <summary>Planned failover — controlled promotion of standby to primary.</summary>
    PlannedFailover
}
