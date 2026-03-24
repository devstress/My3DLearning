namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Service interface for executing a named compensation step during saga rollback.
/// Each compensation step represents the undo logic for a previously committed transaction.
/// </summary>
public interface ICompensationActivityService
{
    /// <summary>
    /// Executes the compensation logic for the named <paramref name="stepName"/>.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga being compensated.</param>
    /// <param name="stepName">The name of the compensation step to execute.</param>
    /// <returns>A task that resolves to <c>true</c> on success, <c>false</c> on failure.</returns>
    Task<bool> CompensateAsync(Guid correlationId, string stepName);
}
