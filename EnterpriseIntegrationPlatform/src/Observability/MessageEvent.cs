using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// A single recorded event in the lifecycle of a message flowing through the platform.
/// Together, the ordered list of events for a given <see cref="CorrelationId"/>
/// forms the full audit trail that answers "where is my message?".
/// </summary>
public sealed record MessageEvent
{
    /// <summary>Unique identifier of this event.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Unique identifier of the message.</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Correlation identifier for end-to-end tracing of a business transaction.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Logical message type (e.g. "OrderShipment").</summary>
    public required string MessageType { get; init; }

    /// <summary>The originating source system.</summary>
    public required string Source { get; init; }

    /// <summary>The processing stage where this event was recorded.</summary>
    public required string Stage { get; init; }

    /// <summary>The delivery status at the time of this event.</summary>
    public required DeliveryStatus Status { get; init; }

    /// <summary>UTC timestamp when this event was recorded.</summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional human-readable details about the event (e.g. error message).</summary>
    public string? Details { get; init; }

    /// <summary>
    /// Optional business key that operators use to look up messages
    /// (e.g. an order number, shipment ID, or invoice reference).
    /// </summary>
    public string? BusinessKey { get; init; }

    /// <summary>W3C trace identifier propagated from OpenTelemetry, if available.</summary>
    public string? TraceId { get; init; }

    /// <summary>W3C span identifier propagated from OpenTelemetry, if available.</summary>
    public string? SpanId { get; init; }
}
