namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Default message validation service.
/// Checks that the payload is non-empty valid JSON and the message type is present.
/// </summary>
public sealed class DefaultMessageValidationService : IMessageValidationService
{
    /// <inheritdoc />
    public Task<MessageValidationResult> ValidateAsync(string messageType, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Task.FromResult(
                MessageValidationResult.Failure("Message type must not be empty."));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(
                MessageValidationResult.Failure("Payload must not be empty."));
        }

        // Basic JSON structure check — payload must start with { or [
        var trimmed = payloadJson.TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
        {
            return Task.FromResult(
                MessageValidationResult.Failure("Payload is not valid JSON."));
        }

        return Task.FromResult(MessageValidationResult.Success);
    }
}
