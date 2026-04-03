using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Record carrying an invalid message's details to the invalid-message channel.
/// </summary>
public record InvalidMessageEnvelope
{
    /// <summary>Original message ID if available, otherwise a new GUID.</summary>
    public required Guid OriginalMessageId { get; init; }

    /// <summary>Raw data or serialized payload that was invalid.</summary>
    public required string RawData { get; init; }

    /// <summary>Topic from which the invalid message originated.</summary>
    public required string SourceTopic { get; init; }

    /// <summary>Reason the message was deemed invalid.</summary>
    public required string Reason { get; init; }

    /// <summary>Timestamp when the message was rejected (UTC).</summary>
    public required DateTimeOffset RejectedAt { get; init; }
}
