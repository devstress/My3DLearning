namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Canonical message envelope used across the entire platform.
/// All messages flowing through the system are wrapped in this envelope.
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
public record IntegrationEnvelope<T>
{
    /// <summary>Unique identifier for this message.</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Correlation identifier for tracing related messages.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Timestamp when the message was created (UTC).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Source system or service that originated the message.</summary>
    public required string Source { get; init; }

    /// <summary>Message type identifier for routing and deserialization.</summary>
    public required string MessageType { get; init; }

    /// <summary>The message payload.</summary>
    public required T Payload { get; init; }

    /// <summary>Optional metadata key-value pairs.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
