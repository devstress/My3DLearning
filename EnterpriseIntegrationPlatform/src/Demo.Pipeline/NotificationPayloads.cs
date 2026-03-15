namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Payload published to the Ack topic when a message is processed successfully.
/// Downstream systems subscribe to the Ack subject to trigger post-processing
/// or send confirmation notifications back to the originating sender.
/// </summary>
/// <param name="OriginalMessageId">The message that was delivered successfully.</param>
/// <param name="CorrelationId">Correlation identifier for end-to-end tracing.</param>
/// <param name="Outcome">Human-readable outcome description.</param>
public record AckPayload(Guid OriginalMessageId, Guid CorrelationId, string Outcome);

/// <summary>
/// Payload published to the Nack topic when message processing fails.
/// Downstream systems subscribe to the Nack subject to trigger compensating
/// transactions or send failure notifications back to the originating sender.
/// </summary>
/// <param name="OriginalMessageId">The message that failed processing.</param>
/// <param name="CorrelationId">Correlation identifier for end-to-end tracing.</param>
/// <param name="Reason">Human-readable failure reason.</param>
public record NackPayload(Guid OriginalMessageId, Guid CorrelationId, string Reason);
