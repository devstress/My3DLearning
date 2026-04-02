using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Rebuilds projection state for an event stream by loading the latest snapshot (if available),
/// then replaying events from the snapshot version forward through the supplied
/// <see cref="IEventProjection{TState}"/>.
/// </summary>
/// <typeparam name="TState">The type of the projected state.</typeparam>
public sealed class EventProjectionEngine<TState> where TState : notnull
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore<TState> _snapshotStore;
    private readonly IEventProjection<TState> _projection;
    private readonly int _snapshotInterval;
    private readonly int _maxEventsPerRead;
    private readonly ILogger<EventProjectionEngine<TState>> _logger;

    /// <summary>Initialises a new instance of <see cref="EventProjectionEngine{TState}"/>.</summary>
    public EventProjectionEngine(
        IEventStore eventStore,
        ISnapshotStore<TState> snapshotStore,
        IEventProjection<TState> projection,
        IOptions<EventSourcingOptions> options,
        ILogger<EventProjectionEngine<TState>> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _projection = projection;
        _snapshotInterval = options.Value.SnapshotInterval;
        _maxEventsPerRead = options.Value.MaxEventsPerRead;
        _logger = logger;
    }

    /// <summary>
    /// Rebuilds the projection state for <paramref name="streamId"/> from its snapshot (if any)
    /// plus all subsequent events. A new snapshot is persisted when the number of events applied
    /// since the last snapshot reaches <see cref="EventSourcingOptions.SnapshotInterval"/>.
    /// </summary>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="initialState">Default state used when no snapshot exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fully rebuilt projection state and its version.</returns>
    public async Task<(TState State, long Version)> RebuildAsync(string streamId, TState initialState, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        var (snapshot, snapshotVersion) = await _snapshotStore.LoadAsync(streamId, cancellationToken);
        var state = snapshot ?? initialState;
        var fromVersion = snapshotVersion + 1;
        var currentVersion = snapshotVersion;
        var eventsSinceSnapshot = 0L;

        _logger.LogDebug("Rebuilding stream '{StreamId}' from version {FromVersion} (snapshot at {SnapshotVersion})",
            streamId, fromVersion, snapshotVersion);

        while (true)
        {
            var events = await _eventStore.ReadStreamAsync(streamId, fromVersion, _maxEventsPerRead, cancellationToken);
            if (events.Count == 0)
            {
                break;
            }

            foreach (var e in events)
            {
                state = await _projection.ProjectAsync(state, e, cancellationToken);
                currentVersion = e.Version;
                eventsSinceSnapshot++;
            }

            fromVersion = events[^1].Version + 1;

            if (events.Count < _maxEventsPerRead)
            {
                break;
            }
        }

        if (eventsSinceSnapshot >= _snapshotInterval && _snapshotInterval > 0)
        {
            await _snapshotStore.SaveAsync(streamId, state, currentVersion, cancellationToken);
            _logger.LogDebug("Saved snapshot for stream '{StreamId}' at version {Version}", streamId, currentVersion);
        }

        return (state, currentVersion);
    }
}
