using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Represents a message as stored in the Message Store for system management queries.
/// </summary>
/// <param name="MessageId">Unique message identifier.</param>
/// <param name="CorrelationId">Correlation identifier linking related messages.</param>
/// <param name="MessageType">Logical message type name.</param>
/// <param name="Source">Originating service or system.</param>
/// <param name="Status">Current delivery status of the message.</param>
/// <param name="RecordedAt">When the message was recorded in the store.</param>
public sealed record MessageStoreEntry(
    Guid MessageId,
    Guid CorrelationId,
    string MessageType,
    string Source,
    DeliveryStatus Status,
    DateTimeOffset RecordedAt);
