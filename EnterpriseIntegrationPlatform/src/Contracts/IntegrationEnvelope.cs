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

    /// <summary>Correlation identifier for tracing a logical business transaction across services.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>
    /// Identifier of the message that directly caused this message to be created.
    /// Null for messages that originate without a parent.
    /// </summary>
    public Guid? CausationId { get; init; }

    /// <summary>Timestamp when the message was created (UTC).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Source system or service that originated the message.</summary>
    public required string Source { get; init; }

    /// <summary>Message type identifier for routing and deserialization.</summary>
    public required string MessageType { get; init; }

    /// <summary>Schema version of the message contract, e.g. "1.0".</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Priority of the message, used by consumers to order processing.</summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;

    /// <summary>The message payload.</summary>
    public required T Payload { get; init; }

    /// <summary>Optional metadata key-value pairs.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a new <see cref="IntegrationEnvelope{T}"/> with a generated <see cref="MessageId"/>
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="source">The name of the originating service or system.</param>
    /// <param name="messageType">The logical message type name.</param>
    /// <param name="correlationId">
    /// Correlation identifier; a new value is generated when not supplied.
    /// </param>
    /// <param name="causationId">
    /// Identifier of the message that caused this message, if applicable.
    /// </param>
    /// <returns>A fully populated <see cref="IntegrationEnvelope{T}"/>.</returns>
    public static IntegrationEnvelope<T> Create(
        T payload,
        string source,
        string messageType,
        Guid? correlationId = null,
        Guid? causationId = null) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            CausationId = causationId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Payload = payload,
        };
}
