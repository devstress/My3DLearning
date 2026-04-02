namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Projects an event onto an immutable state object, producing the next version of that state.
/// Implementations must be pure functions with no side-effects so that replays are deterministic.
/// </summary>
/// <typeparam name="TState">The type of the projected state.</typeparam>
public interface IEventProjection<TState>
{
    /// <summary>
    /// Applies <paramref name="envelope"/> to the current <paramref name="state"/> and returns the new state.
    /// </summary>
    /// <param name="state">Current projection state.</param>
    /// <param name="envelope">Event envelope to project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated projection state.</returns>
    Task<TState> ProjectAsync(TState state, EventEnvelope envelope, CancellationToken cancellationToken = default);
}
