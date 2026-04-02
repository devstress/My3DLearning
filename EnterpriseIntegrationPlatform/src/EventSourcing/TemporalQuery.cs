namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Static helper methods for temporal (point-in-time) event stream queries.
/// </summary>
public static class TemporalQuery
{
    /// <summary>
    /// Replays a stream through the given projection up to and including
    /// <paramref name="pointInTime"/>, reconstructing the state as it existed at that moment.
    /// </summary>
    /// <typeparam name="TState">The type of the projected state.</typeparam>
    /// <param name="eventStore">Event store to read events from.</param>
    /// <param name="projection">Projection that folds events into state.</param>
    /// <param name="streamId">Event stream identifier.</param>
    /// <param name="pointInTime">Inclusive upper-bound timestamp for the replay.</param>
    /// <param name="initialState">Default state to start from before any events are applied.</param>
    /// <param name="maxEventsPerRead">Maximum events per read batch. Default is <c>1000</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected state at the specified point in time and the version of the last applied event.</returns>
    public static async Task<(TState State, long Version)> ReplayToPointInTimeAsync<TState>(
        IEventStore eventStore,
        IEventProjection<TState> projection,
        string streamId,
        DateTimeOffset pointInTime,
        TState initialState,
        int maxEventsPerRead = 1000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        var state = initialState;
        var fromVersion = 1L;
        var lastVersion = 0L;

        while (true)
        {
            var events = await eventStore.ReadStreamAsync(streamId, fromVersion, maxEventsPerRead, cancellationToken);
            if (events.Count == 0)
            {
                break;
            }

            foreach (var e in events)
            {
                if (e.Timestamp > pointInTime)
                {
                    return (state, lastVersion);
                }

                state = await projection.ProjectAsync(state, e, cancellationToken);
                lastVersion = e.Version;
            }

            fromVersion = events[^1].Version + 1;

            if (events.Count < maxEventsPerRead)
            {
                break;
            }
        }

        return (state, lastVersion);
    }
}
