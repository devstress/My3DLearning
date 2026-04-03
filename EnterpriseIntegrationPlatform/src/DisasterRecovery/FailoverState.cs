namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Represents the current state of a failover region.
/// </summary>
public enum FailoverState
{
    /// <summary>Region is operating as the primary, serving all traffic.</summary>
    Primary,

    /// <summary>Region is on standby, receiving replication data but not serving traffic.</summary>
    Standby,

    /// <summary>Failover is in progress — traffic is being redirected to the standby region.</summary>
    FailingOver,

    /// <summary>Region is actively serving traffic after a failover event.</summary>
    Active,

    /// <summary>Region is recovering and re-synchronising data after a failover.</summary>
    Recovering,

    /// <summary>Region is degraded — operating with reduced capacity or data lag.</summary>
    Degraded,

    /// <summary>Region is offline and unreachable.</summary>
    Offline
}
