namespace Terranes.Contracts.Enums;

/// <summary>
/// Health status of a system component.
/// </summary>
public enum HealthStatus
{
    /// <summary>Component is operational.</summary>
    Healthy,

    /// <summary>Component is degraded but functional.</summary>
    Degraded,

    /// <summary>Component is not functional.</summary>
    Unhealthy
}
