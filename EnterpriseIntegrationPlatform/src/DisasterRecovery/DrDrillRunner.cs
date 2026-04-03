using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Orchestrates disaster recovery drills by simulating failures, executing failover,
/// measuring recovery metrics, and validating objectives.
/// </summary>
public sealed class DrDrillRunner : IDrDrillRunner
{
    private readonly IFailoverManager _failoverManager;
    private readonly IReplicationManager _replicationManager;
    private readonly IRecoveryPointValidator _validator;
    private readonly ILogger<DrDrillRunner> _logger;
    private readonly DisasterRecoveryOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentQueue<DrDrillResult> _history = new();
    private volatile int _historyCount;

    /// <summary>
    /// Initialises a new instance of <see cref="DrDrillRunner"/>.
    /// </summary>
    /// <param name="failoverManager">Failover manager.</param>
    /// <param name="replicationManager">Replication manager.</param>
    /// <param name="validator">Recovery point validator.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Disaster recovery options.</param>
    /// <param name="timeProvider">Time provider for testability. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    public DrDrillRunner(
        IFailoverManager failoverManager,
        IReplicationManager replicationManager,
        IRecoveryPointValidator validator,
        ILogger<DrDrillRunner> logger,
        IOptions<DisasterRecoveryOptions> options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(failoverManager);
        ArgumentNullException.ThrowIfNull(replicationManager);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _failoverManager = failoverManager;
        _replicationManager = replicationManager;
        _validator = validator;
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<DrDrillResult> RunDrillAsync(DrDrillScenario scenario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = _timeProvider.GetUtcNow();
        _logger.LogWarning(
            "Starting DR drill: {ScenarioName} ({DrillType}) — target={Target}, failover={Failover}",
            scenario.Name, scenario.DrillType, scenario.TargetRegionId, scenario.FailoverRegionId);

        try
        {
            // Phase 1: Detection — measure how quickly we detect the simulated failure
            var detectionStart = _timeProvider.GetUtcNow();
            var regions = await _failoverManager.GetAllRegionsAsync(cancellationToken);
            var targetExists = regions.Any(r =>
                string.Equals(r.RegionId, scenario.TargetRegionId, StringComparison.OrdinalIgnoreCase));
            var failoverExists = regions.Any(r =>
                string.Equals(r.RegionId, scenario.FailoverRegionId, StringComparison.OrdinalIgnoreCase));

            if (!targetExists || !failoverExists)
            {
                var errorMsg = $"Drill aborted: region(s) not found (target={targetExists}, failover={failoverExists}).";
                _logger.LogError("{Error}", errorMsg);
                return RecordResult(CreateFailedResult(scenario, startedAt, errorMsg));
            }

            var detectionTime = _timeProvider.GetUtcNow() - detectionStart;

            // Phase 2: Get replication status to measure potential data loss
            var replicationStatus = await _replicationManager.GetStatusAsync(
                scenario.TargetRegionId, scenario.FailoverRegionId, cancellationToken);
            var dataLoss = replicationStatus.Lag;

            // Phase 3: Execute failover
            var failoverStart = _timeProvider.GetUtcNow();
            var failoverResult = await _failoverManager.FailoverAsync(scenario.FailoverRegionId, cancellationToken);
            var failoverTime = _timeProvider.GetUtcNow() - failoverStart;

            if (!failoverResult.Success)
            {
                _logger.LogError("DR drill failover failed: {Error}", failoverResult.ErrorMessage);
                return RecordResult(CreateFailedResult(scenario, startedAt,
                    $"Failover failed: {failoverResult.ErrorMessage}"));
            }

            // Phase 4: Validate recovery objectives
            var objectives = await _validator.GetObjectivesAsync(cancellationToken);
            RecoveryPointValidationResult? validationResult = null;
            if (objectives.Count > 0)
            {
                var results = await _validator.ValidateAllAsync(dataLoss, failoverTime, cancellationToken);
                validationResult = results.FirstOrDefault();
            }

            // Phase 5: Failback if requested
            var failbackCompleted = false;
            if (scenario.AutoFailback)
            {
                var failbackResult = await _failoverManager.FailbackAsync(scenario.TargetRegionId, cancellationToken);
                failbackCompleted = failbackResult.Success;
                if (!failbackCompleted)
                {
                    _logger.LogWarning("DR drill failback failed: {Error}", failbackResult.ErrorMessage);
                }
            }

            var completedAt = _timeProvider.GetUtcNow();
            var totalDuration = completedAt - startedAt;

            _logger.LogWarning(
                "DR drill completed: {ScenarioName} — detection={Detection}ms, failover={Failover}ms, total={Total}ms, success=true",
                scenario.Name, detectionTime.TotalMilliseconds, failoverTime.TotalMilliseconds, totalDuration.TotalMilliseconds);

            return RecordResult(new DrDrillResult
            {
                Scenario = scenario,
                Success = true,
                DetectionTime = detectionTime,
                FailoverTime = failoverTime,
                TotalDuration = totalDuration,
                DataLoss = dataLoss,
                ValidationResult = validationResult,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                FailbackCompleted = failbackCompleted
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DR drill {ScenarioName} failed with exception", scenario.Name);
            return RecordResult(CreateFailedResult(scenario, startedAt, ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DrDrillResult>> GetDrillHistoryAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DrDrillResult> results = _history
            .Reverse()
            .Take(limit)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(results);
    }

    /// <inheritdoc />
    public Task<DrDrillResult?> GetLastDrillResultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _history.TryPeek(out var last);
        // ConcurrentQueue is FIFO, but we want the last element
        var result = _history.LastOrDefault();
        return Task.FromResult(result);
    }

    private DrDrillResult RecordResult(DrDrillResult result)
    {
        _history.Enqueue(result);
        Interlocked.Increment(ref _historyCount);

        // Trim history if over limit
        while (_historyCount > _options.MaxDrillHistorySize && _history.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _historyCount);
        }

        return result;
    }

    private DrDrillResult CreateFailedResult(DrDrillScenario scenario, DateTimeOffset startedAt, string errorMessage)
    {
        var completedAt = _timeProvider.GetUtcNow();
        return new DrDrillResult
        {
            Scenario = scenario,
            Success = false,
            DetectionTime = TimeSpan.Zero,
            FailoverTime = TimeSpan.Zero,
            TotalDuration = completedAt - startedAt,
            DataLoss = TimeSpan.Zero,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ErrorMessage = errorMessage
        };
    }
}
