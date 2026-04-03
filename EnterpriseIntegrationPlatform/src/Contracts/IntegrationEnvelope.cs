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
    /// Return Address — the topic or subject that the sender expects replies to be published to.
    /// Null when no reply is expected. Used by the Request-Reply pattern.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Message Expiration — the point in time after which this message should be considered stale.
    /// Processing steps must check this value and route expired messages to the Dead Letter Queue
    /// with reason "expired". Null means the message never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Message Sequence — zero-based position of this message within a sequence (e.g. produced by a Splitter).
    /// Null for standalone (non-sequenced) messages.
    /// </summary>
    public int? SequenceNumber { get; init; }

    /// <summary>
    /// Total number of messages in the sequence this message belongs to.
    /// Null for standalone (non-sequenced) messages.
    /// </summary>
    public int? TotalCount { get; init; }

    /// <summary>
    /// Message Intent — distinguishes between Command, Document, and Event messages
    /// as defined by the Enterprise Integration Patterns. Null when intent is not specified.
    /// </summary>
    public MessageIntent? Intent { get; init; }

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

    /// <summary>
    /// Returns <c>true</c> when <see cref="ExpiresAt"/> is set and the current UTC time
    /// is past the expiration timestamp.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
