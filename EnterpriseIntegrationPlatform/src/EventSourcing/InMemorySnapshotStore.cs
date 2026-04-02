using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ISnapshotStore{TState}"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="TState">The type of the projected state that is snapshotted.</typeparam>
public sealed class InMemorySnapshotStore<TState> : ISnapshotStore<TState>
{
    private readonly ConcurrentDictionary<string, (TState State, long Version)> _snapshots = new();

    /// <inheritdoc />
    public Task SaveAsync(string streamId, TState state, long version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        _snapshots[streamId] = (state, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<(TState? State, long Version)> LoadAsync(string streamId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_snapshots.TryGetValue(streamId, out var entry))
        {
            return Task.FromResult<(TState? State, long Version)>((entry.State, entry.Version));
        }

        return Task.FromResult<(TState? State, long Version)>((default, 0L));
    }
}
