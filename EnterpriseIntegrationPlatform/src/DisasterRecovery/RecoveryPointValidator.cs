using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.DisasterRecovery;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IRecoveryPointValidator"/>.
/// Validates recovery objectives against current replication and failover metrics.
/// </summary>
public sealed class RecoveryPointValidator : IRecoveryPointValidator
{
    private readonly ConcurrentDictionary<string, RecoveryObjective> _objectives = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<RecoveryPointValidator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="RecoveryPointValidator"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="timeProvider">Time provider for testability. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    public RecoveryPointValidator(
        ILogger<RecoveryPointValidator> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task RegisterObjectiveAsync(RecoveryObjective objective, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objective);
        cancellationToken.ThrowIfCancellationRequested();

        _objectives[objective.ObjectiveId] = objective;
        _logger.LogInformation(
            "Recovery objective registered: {ObjectiveId} (RPO={Rpo}, RTO={Rto})",
            objective.ObjectiveId, objective.Rpo, objective.Rto);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RecoveryObjective>> GetObjectivesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RecoveryObjective> result = _objectives.Values.ToList().AsReadOnly();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<RecoveryPointValidationResult> ValidateAsync(
        string objectiveId,
        TimeSpan currentLag,
        TimeSpan lastFailoverDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectiveId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_objectives.TryGetValue(objectiveId, out var objective))
        {
            throw new KeyNotFoundException($"Recovery objective '{objectiveId}' not found.");
        }

        var result = Validate(objective, currentLag, lastFailoverDuration);

        _logger.LogInformation(
            "Objective {ObjectiveId} validation: RPO {RpoStatus}, RTO {RtoStatus} (lag={Lag}, failover={Failover})",
            objectiveId,
            result.RpoMet ? "MET" : "VIOLATED",
            result.RtoMet ? "MET" : "VIOLATED",
            currentLag,
            lastFailoverDuration);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RecoveryPointValidationResult>> ValidateAllAsync(
        TimeSpan currentLag,
        TimeSpan lastFailoverDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = _objectives.Values
            .Select(o => Validate(o, currentLag, lastFailoverDuration))
            .ToList()
            .AsReadOnly();

        var failed = results.Count(r => !r.Passed);
        if (failed > 0)
        {
            _logger.LogWarning("{FailedCount} of {TotalCount} recovery objectives not met", failed, results.Count);
        }

        return Task.FromResult<IReadOnlyList<RecoveryPointValidationResult>>(results);
    }

    private RecoveryPointValidationResult Validate(
        RecoveryObjective objective,
        TimeSpan currentLag,
        TimeSpan lastFailoverDuration)
    {
        return new RecoveryPointValidationResult
        {
            Objective = objective,
            RpoMet = currentLag <= objective.Rpo,
            RtoMet = lastFailoverDuration <= objective.Rto,
            CurrentLag = currentLag,
            LastFailoverDuration = lastFailoverDuration,
            ValidatedAt = _timeProvider.GetUtcNow()
        };
    }
}
