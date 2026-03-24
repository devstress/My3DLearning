using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Default implementation of <see cref="ICompensationActivityService"/> that logs
/// each compensation step. In production, replace or extend with real rollback logic
/// per step name (e.g. reverse a database write, send a cancellation request).
/// </summary>
public sealed class DefaultCompensationActivityService : ICompensationActivityService
{
    private readonly ILogger<DefaultCompensationActivityService> _logger;

    /// <summary>Initialises a new instance of <see cref="DefaultCompensationActivityService"/>.</summary>
    public DefaultCompensationActivityService(ILogger<DefaultCompensationActivityService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> CompensateAsync(Guid correlationId, string stepName)
    {
        _logger.LogInformation(
            "[SagaCompensation] Executing compensation for step '{StepName}' on correlation {CorrelationId}",
            stepName, correlationId);

        // Real implementations would dispatch a reversal command here.
        // The result signals whether the compensation succeeded.
        return Task.FromResult(true);
    }
}
