using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for platform observability — structured logging, health checks, metrics, audit trail.
/// </summary>
public interface IObservabilityService
{
    /// <summary>Records an audit log entry.</summary>
    Task<AuditLogEntry> LogAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Gets audit log entries for an entity.</summary>
    Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>Gets audit log entries for a user.</summary>
    Task<IReadOnlyList<AuditLogEntry>> GetUserAuditLogAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Runs all health checks and returns results.</summary>
    Task<IReadOnlyList<HealthCheckResult>> RunHealthChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a custom metric.</summary>
    Task RecordMetricAsync(string metricName, double value, CancellationToken cancellationToken = default);

    /// <summary>Gets recorded metric values.</summary>
    Task<IReadOnlyList<(string Name, double Value, DateTimeOffset Timestamp)>> GetMetricsAsync(string metricName, CancellationToken cancellationToken = default);
}
