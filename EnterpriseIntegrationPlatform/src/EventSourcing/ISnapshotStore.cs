namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Stores and retrieves projection snapshots for event streams, enabling efficient
/// state reconstruction without replaying every event from the beginning.
/// </summary>
/// <typeparam name="TState">The type of the projected state that is snapshotted.</typeparam>
public interface ISnapshotStore<TState>
{
    /// <summary>
    /// Persists a snapshot of the projection state at the given stream version.
    /// Overwrites any existing snapshot for the same stream.
    /// </summary>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="state">Projection state to snapshot.</param>
    /// <param name="version">Stream version at which the snapshot was taken.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(string streamId, TState state, long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the most recent snapshot for the given stream.
    /// </summary>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple of the snapshotted state and its version, or <c>(default, 0)</c> when no snapshot exists.
    /// </returns>
    Task<(TState? State, long Version)> LoadAsync(string streamId, CancellationToken cancellationToken = default);
}
