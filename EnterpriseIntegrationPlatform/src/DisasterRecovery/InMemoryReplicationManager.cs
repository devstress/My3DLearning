using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IReplicationManager"/>.
/// Tracks replication progress and calculates lag between regions.
/// </summary>
public sealed class InMemoryReplicationManager : IReplicationManager
{
    private readonly ConcurrentDictionary<string, ReplicationPairState> _pairs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryReplicationManager> _logger;
    private readonly DisasterRecoveryOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="InMemoryReplicationManager"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Disaster recovery options.</param>
    /// <param name="timeProvider">Time provider for testability. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    public InMemoryReplicationManager(
        ILogger<InMemoryReplicationManager> logger,
        IOptions<DisasterRecoveryOptions> options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<ReplicationStatus> GetStatusAsync(string sourceRegionId, string targetRegionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRegionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRegionId);
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakePairKey(sourceRegionId, targetRegionId);
        var now = _timeProvider.GetUtcNow();

        if (!_pairs.TryGetValue(key, out var state))
        {
            return Task.FromResult(new ReplicationStatus
            {
                SourceRegionId = sourceRegionId,
                TargetRegionId = targetRegionId,
                Lag = TimeSpan.Zero,
                PendingItems = 0,
                IsHealthy = true,
                CapturedAt = now,
                LastReplicatedSequence = 0
            });
        }

        var pending = state.SourceSequence - state.ReplicatedSequence;
        var lag = TimeSpan.FromMilliseconds(pending * _options.PerItemReplicationTime.TotalMilliseconds);
        var isHealthy = lag <= _options.MaxReplicationLag;

        return Task.FromResult(new ReplicationStatus
        {
            SourceRegionId = sourceRegionId,
            TargetRegionId = targetRegionId,
            Lag = lag,
            PendingItems = Math.Max(0, pending),
            IsHealthy = isHealthy,
            CapturedAt = now,
            LastReplicatedSequence = state.ReplicatedSequence
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReplicationStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _timeProvider.GetUtcNow();
        var results = new List<ReplicationStatus>();

        foreach (var kvp in _pairs)
        {
            var state = kvp.Value;
            var pending = state.SourceSequence - state.ReplicatedSequence;
            var lag = TimeSpan.FromMilliseconds(pending * _options.PerItemReplicationTime.TotalMilliseconds);
            var isHealthy = lag <= _options.MaxReplicationLag;

            results.Add(new ReplicationStatus
            {
                SourceRegionId = state.SourceRegionId,
                TargetRegionId = state.TargetRegionId,
                Lag = lag,
                PendingItems = Math.Max(0, pending),
                IsHealthy = isHealthy,
                CapturedAt = now,
                LastReplicatedSequence = state.ReplicatedSequence
            });
        }

        return Task.FromResult<IReadOnlyList<ReplicationStatus>>(results.AsReadOnly());
    }

    /// <inheritdoc />
    public Task ReportReplicationAsync(string sourceRegionId, string targetRegionId, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRegionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRegionId);
        ArgumentOutOfRangeException.ThrowIfNegative(sequenceNumber);
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakePairKey(sourceRegionId, targetRegionId);
        _pairs.AddOrUpdate(
            key,
            _ => new ReplicationPairState(sourceRegionId, targetRegionId, 0, sequenceNumber),
            (_, existing) => existing with { ReplicatedSequence = Math.Max(existing.ReplicatedSequence, sequenceNumber) });

        _logger.LogDebug(
            "Replication reported: {Source} → {Target}, sequence {Sequence}",
            sourceRegionId, targetRegionId, sequenceNumber);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportSourceProgressAsync(string sourceRegionId, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRegionId);
        ArgumentOutOfRangeException.ThrowIfNegative(sequenceNumber);
        cancellationToken.ThrowIfCancellationRequested();

        // Update all pairs where this region is the source
        foreach (var key in _pairs.Keys)
        {
            if (_pairs.TryGetValue(key, out var state) &&
                string.Equals(state.SourceRegionId, sourceRegionId, StringComparison.OrdinalIgnoreCase))
            {
                _pairs[key] = state with { SourceSequence = Math.Max(state.SourceSequence, sequenceNumber) };
            }
        }

        _logger.LogDebug("Source progress reported: {Source}, sequence {Sequence}", sourceRegionId, sequenceNumber);
        return Task.CompletedTask;
    }

    private static string MakePairKey(string source, string target) =>
        $"{source.ToUpperInvariant()}→{target.ToUpperInvariant()}";

    /// <summary>
    /// Internal state tracking for a replication pair.
    /// </summary>
    internal sealed record ReplicationPairState(
        string SourceRegionId,
        string TargetRegionId,
        long SourceSequence,
        long ReplicatedSequence);
}
