namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Represents a message that could not be processed successfully.
/// Produced by any service that encounters an unrecoverable error and routes the
/// original message to the dead-letter store.
/// </summary>
public record FaultEnvelope
{
    /// <summary>Unique identifier of this fault record.</summary>
    public required Guid FaultId { get; init; }

    /// <summary><see cref="IntegrationEnvelope{T}.MessageId"/> of the message that faulted.</summary>
    public required Guid OriginalMessageId { get; init; }

    /// <summary><see cref="IntegrationEnvelope{T}.CorrelationId"/> carried over from the original message.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary><see cref="IntegrationEnvelope{T}.MessageType"/> of the original message.</summary>
    public required string OriginalMessageType { get; init; }

    /// <summary>Name of the service or component where the fault occurred.</summary>
    public required string FaultedBy { get; init; }

    /// <summary>Human-readable description of why the message could not be processed.</summary>
    public required string FaultReason { get; init; }

    /// <summary>UTC timestamp when the fault was recorded.</summary>
    public required DateTimeOffset FaultedAt { get; init; }

    /// <summary>Number of processing attempts made before declaring the message faulted.</summary>
    public required int RetryCount { get; init; }

    /// <summary>Optional exception details (type, message, stack trace) for diagnostics.</summary>
    public string? ErrorDetails { get; init; }

    /// <summary>The serialised original message envelope, preserved for replay or manual reprocessing.</summary>
    public string? OriginalPayloadJson { get; init; }

    /// <summary>
    /// Creates a <see cref="FaultEnvelope"/> from an existing envelope and an exception.
    /// </summary>
    /// <typeparam name="T">Payload type of the faulted message.</typeparam>
    /// <param name="original">The envelope that could not be processed.</param>
    /// <param name="faultedBy">Name of the faulting service.</param>
    /// <param name="reason">Human-readable fault reason.</param>
    /// <param name="retryCount">Number of attempts already made.</param>
    /// <param name="exception">The exception that caused the fault, if available.</param>
    /// <returns>A fully populated <see cref="FaultEnvelope"/>.</returns>
    public static FaultEnvelope Create<T>(
        IntegrationEnvelope<T> original,
        string faultedBy,
        string reason,
        int retryCount,
        Exception? exception = null) =>
        new()
        {
            FaultId = Guid.NewGuid(),
            OriginalMessageId = original.MessageId,
            CorrelationId = original.CorrelationId,
            OriginalMessageType = original.MessageType,
            FaultedBy = faultedBy,
            FaultReason = reason,
            FaultedAt = DateTimeOffset.UtcNow,
            RetryCount = retryCount,
            ErrorDetails = exception is null
                ? null
                : $"{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}",
        };
}
