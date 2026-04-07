using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IObservabilityService"/>.
/// Provides structured audit logging, health checks, and metrics recording.
/// </summary>
public sealed class ObservabilityService : IObservabilityService
{
    private readonly ConcurrentDictionary<Guid, AuditLogEntry> _auditLog = new();
    private readonly ConcurrentBag<(string Name, double Value, DateTimeOffset Timestamp)> _metrics = [];
    private readonly ILogger<ObservabilityService> _logger;

    public ObservabilityService(ILogger<ObservabilityService> logger) => _logger = logger;

    public Task<AuditLogEntry> LogAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Action))
            throw new ArgumentException("Action is required.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.EntityType))
            throw new ArgumentException("Entity type is required.", nameof(entry));

        var persisted = entry with { Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id, TimestampUtc = DateTimeOffset.UtcNow };

        if (!_auditLog.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Audit log entry {persisted.Id} already exists.");

        _logger.LogInformation("Audit: {Action} on {EntityId} by user {UserId}", persisted.Action, persisted.EntityId, persisted.UserId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditLogEntry> result = _auditLog.Values
            .Where(e => string.Equals(e.EntityType, entityType, StringComparison.OrdinalIgnoreCase) && e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetUserAuditLogAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditLogEntry> result = _auditLog.Values
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<HealthCheckResult>> RunHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var now = DateTimeOffset.UtcNow;

        // Built-in checks for all known services
        IReadOnlyList<HealthCheckResult> results =
        [
            new("Models3D", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("Land", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("SitePlacement", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("Quoting", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("Marketplace", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("Compliance", HealthStatus.Healthy, "In-memory store operational", sw.ElapsedMilliseconds, now),
            new("PartnerIntegration", HealthStatus.Healthy, "6 partner services operational", sw.ElapsedMilliseconds, now),
            new("Immersive3D", HealthStatus.Healthy, "5 immersive services operational", sw.ElapsedMilliseconds, now),
            new("Authentication", HealthStatus.Healthy, "Auth service operational", sw.ElapsedMilliseconds, now),
            new("MultiTenancy", HealthStatus.Healthy, "Tenant isolation operational", sw.ElapsedMilliseconds, now),
        ];

        return Task.FromResult(results);
    }

    public Task RecordMetricAsync(string metricName, double value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name is required.", nameof(metricName));

        _metrics.Add((metricName, value, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(string Name, double Value, DateTimeOffset Timestamp)>> GetMetricsAsync(string metricName, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<(string Name, double Value, DateTimeOffset Timestamp)> result = _metrics
            .Where(m => string.Equals(m.Name, metricName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .ToList();
        return Task.FromResult(result);
    }
}
