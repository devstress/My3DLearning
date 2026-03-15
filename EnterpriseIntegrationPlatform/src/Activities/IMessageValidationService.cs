namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Service interface for validating integration messages.
/// Implementations live in the Activities project; Temporal activity wrappers
/// live in Workflow.Temporal.
/// </summary>
public interface IMessageValidationService
{
    /// <summary>
    /// Validates that a message payload is acceptable for processing.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="payloadJson">The JSON-serialised payload.</param>
    /// <returns>A validation result indicating success or the reason for failure.</returns>
    Task<MessageValidationResult> ValidateAsync(string messageType, string payloadJson);
}

/// <summary>
/// Result of a message validation check.
/// </summary>
/// <param name="IsValid">Whether the message passed validation.</param>
/// <param name="Reason">Explanation when validation fails; null on success.</param>
public record MessageValidationResult(bool IsValid, string? Reason = null)
{
    /// <summary>A successful validation result.</summary>
    public static MessageValidationResult Success { get; } = new(true);

    /// <summary>Creates a failed validation result with the given reason.</summary>
    public static MessageValidationResult Failure(string reason) => new(false, reason);
}
