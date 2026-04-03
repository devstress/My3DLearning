using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IFailoverManager"/>.
/// Manages region registration, failover, and failback with full state tracking.
/// </summary>
public sealed class InMemoryFailoverManager : IFailoverManager
{
    private readonly ConcurrentDictionary<string, RegionInfo> _regions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryFailoverManager> _logger;
    private readonly DisasterRecoveryOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly object _failoverLock = new();

    /// <summary>
    /// Initialises a new instance of <see cref="InMemoryFailoverManager"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Disaster recovery options.</param>
    /// <param name="timeProvider">Time provider for testability. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    public InMemoryFailoverManager(
        ILogger<InMemoryFailoverManager> logger,
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
    public Task RegisterRegionAsync(RegionInfo region, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(region);
        cancellationToken.ThrowIfCancellationRequested();

        _regions.AddOrUpdate(region.RegionId, region, (_, _) => region);
        _logger.LogInformation("Region {RegionId} registered with state {State}", region.RegionId, region.State);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RegionInfo?> GetPrimaryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var primary = _regions.Values.FirstOrDefault(r => r.IsPrimary);
        return Task.FromResult(primary);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RegionInfo>> GetAllRegionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RegionInfo> regions = _regions.Values.ToList().AsReadOnly();
        return Task.FromResult(regions);
    }

    /// <inheritdoc />
    public Task<FailoverResult> FailoverAsync(string targetRegionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRegionId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_failoverLock)
        {
            var start = _timeProvider.GetUtcNow();

            if (!_regions.TryGetValue(targetRegionId, out var target))
            {
                return Task.FromResult(new FailoverResult
                {
                    Success = false,
                    PromotedRegionId = targetRegionId,
                    DemotedRegionId = string.Empty,
                    Duration = TimeSpan.Zero,
                    CompletedAt = start,
                    ErrorMessage = $"Target region '{targetRegionId}' is not registered."
                });
            }

            var currentPrimary = _regions.Values.FirstOrDefault(r => r.IsPrimary);
            if (currentPrimary is null)
            {
                return Task.FromResult(new FailoverResult
                {
                    Success = false,
                    PromotedRegionId = targetRegionId,
                    DemotedRegionId = string.Empty,
                    Duration = TimeSpan.Zero,
                    CompletedAt = start,
                    ErrorMessage = "No current primary region found."
                });
            }

            if (string.Equals(currentPrimary.RegionId, targetRegionId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new FailoverResult
                {
                    Success = false,
                    PromotedRegionId = targetRegionId,
                    DemotedRegionId = currentPrimary.RegionId,
                    Duration = TimeSpan.Zero,
                    CompletedAt = start,
                    ErrorMessage = "Target region is already the primary."
                });
            }

            // Demote current primary
            var demoted = currentPrimary with { State = FailoverState.Standby, LastHealthCheck = start };
            _regions[currentPrimary.RegionId] = demoted;

            // Promote target
            var promoted = target with { State = FailoverState.Primary, LastHealthCheck = start };
            _regions[targetRegionId] = promoted;

            var end = _timeProvider.GetUtcNow();
            var duration = end - start;

            _logger.LogWarning(
                "Failover completed: {DemotedRegion} → standby, {PromotedRegion} → primary in {Duration}ms",
                currentPrimary.RegionId, targetRegionId, duration.TotalMilliseconds);

            return Task.FromResult(new FailoverResult
            {
                Success = true,
                PromotedRegionId = targetRegionId,
                DemotedRegionId = currentPrimary.RegionId,
                Duration = duration,
                CompletedAt = end
            });
        }
    }

    /// <inheritdoc />
    public Task<FailoverResult> FailbackAsync(string originalPrimaryRegionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPrimaryRegionId);
        // Failback is semantically the same as failover — promote the original primary
        return FailoverAsync(originalPrimaryRegionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateHealthCheckAsync(string regionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_regions.TryGetValue(regionId, out var region))
        {
            _regions[regionId] = region with { LastHealthCheck = _timeProvider.GetUtcNow() };
            _logger.LogDebug("Health check updated for region {RegionId}", regionId);
        }
        else
        {
            _logger.LogWarning("Health check update failed: region {RegionId} not found", regionId);
        }

        return Task.CompletedTask;
    }
}
