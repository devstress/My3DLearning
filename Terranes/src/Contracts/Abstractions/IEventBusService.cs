using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// In-memory event bus for cross-service communication.
/// </summary>
public interface IEventBusService
{
    /// <summary>Publishes an event to a topic.</summary>
    Task<PlatformEvent> PublishAsync(string topic, string payload, Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all events for a topic.</summary>
    Task<IReadOnlyList<PlatformEvent>> GetEventsForTopicAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all events correlated by a specific ID.</summary>
    Task<IReadOnlyList<PlatformEvent>> GetEventsForCorrelationAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>Gets total event count across all topics.</summary>
    Task<int> GetTotalEventCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists all known topics with event counts.</summary>
    Task<IReadOnlyDictionary<string, int>> GetTopicSummaryAsync(CancellationToken cancellationToken = default);
}
