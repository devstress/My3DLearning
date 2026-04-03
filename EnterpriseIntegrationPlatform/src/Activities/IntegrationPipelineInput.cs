namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Input for the <c>IntegrationPipelineWorkflow</c> — carries the full message data
/// so that every step (persist, validate, ack/nack) executes as a Temporal activity
/// inside a single durable workflow. This replaces the previous non-atomic
/// orchestration that ran side-effects outside Temporal.
/// </summary>
/// <param name="MessageId">Unique message identifier.</param>
/// <param name="CorrelationId">Correlation identifier for end-to-end tracing.</param>
/// <param name="CausationId">Identifier of the message that caused this one, if any.</param>
/// <param name="Timestamp">UTC timestamp when the message was created.</param>
/// <param name="Source">Originating source system.</param>
/// <param name="MessageType">Logical message type name.</param>
/// <param name="SchemaVersion">Schema version of the message contract.</param>
/// <param name="Priority">Message priority (int-mapped from <see cref="Contracts.MessagePriority"/>).</param>
/// <param name="PayloadJson">Serialised message payload.</param>
/// <param name="MetadataJson">Serialised metadata dictionary, if any.</param>
/// <param name="AckSubject">NATS subject for Ack notifications.</param>
/// <param name="NackSubject">NATS subject for Nack notifications.</param>
public sealed record IntegrationPipelineInput(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset Timestamp,
    string Source,
    string MessageType,
    string SchemaVersion,
    int Priority,
    string PayloadJson,
    string? MetadataJson,
    string AckSubject,
    string NackSubject);
