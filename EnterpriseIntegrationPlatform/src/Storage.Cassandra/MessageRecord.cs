using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Persistent message record stored in Cassandra.
/// This is the denormalised row stored across the <c>messages_by_correlation_id</c>
/// and <c>messages_by_id</c> tables.
/// </summary>
public sealed record MessageRecord
{
    /// <summary>Unique identifier of the message.</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Correlation identifier for end-to-end tracing of a business transaction.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Identifier of the message that caused this message, if applicable.</summary>
    public Guid? CausationId { get; init; }

    /// <summary>UTC timestamp when the message was recorded.</summary>
    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>The originating source system.</summary>
    public required string Source { get; init; }

    /// <summary>Logical message type name.</summary>
    public required string MessageType { get; init; }

    /// <summary>Schema version of the message contract.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Message priority.</summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;

    /// <summary>Serialised message payload (JSON).</summary>
    public required string PayloadJson { get; init; }

    /// <summary>Serialised metadata dictionary (JSON).</summary>
    public string? MetadataJson { get; init; }

    /// <summary>Current delivery status of the message.</summary>
    public DeliveryStatus DeliveryStatus { get; init; } = DeliveryStatus.Pending;
}
