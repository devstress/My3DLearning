namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an in-memory event published on the platform event bus.
/// </summary>
/// <param name="Id">Unique event identifier.</param>
/// <param name="Topic">Event topic (e.g. "journey.stage-changed", "quote.completed").</param>
/// <param name="Payload">Serialised event payload (JSON string).</param>
/// <param name="CorrelationId">Correlation ID for tracing across services.</param>
/// <param name="PublishedAtUtc">UTC timestamp when the event was published.</param>
public sealed record PlatformEvent(
    Guid Id,
    string Topic,
    string Payload,
    Guid CorrelationId,
    DateTimeOffset PublishedAtUtc);
