namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Immutable envelope that wraps a domain event with stream metadata, versioning, and audit information.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="StreamId">Identifier of the event stream (aggregate) this event belongs to.</param>
/// <param name="EventType">Discriminator string identifying the type of domain event (e.g. <c>"OrderCreated"</c>).</param>
/// <param name="Data">JSON-serialised payload of the domain event.</param>
/// <param name="Version">Monotonically increasing version number within the stream, starting at 1.</param>
/// <param name="Timestamp">Point in time at which the event was recorded.</param>
/// <param name="Metadata">Arbitrary key/value metadata (correlation IDs, user info, etc.).</param>
public sealed record EventEnvelope(
    Guid EventId,
    string StreamId,
    string EventType,
    string Data,
    long Version,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Metadata);
