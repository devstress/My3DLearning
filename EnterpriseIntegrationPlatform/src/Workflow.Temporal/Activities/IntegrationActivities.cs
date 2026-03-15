using Temporalio.Activities;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

/// <summary>
/// Temporal activity definitions for integration message processing.
/// Each method decorated with <see cref="ActivityAttribute"/> is registered
/// with the Temporal worker and can be invoked from workflow code.
/// </summary>
/// <remarks>
/// Requires <see cref="IMessageValidationService"/> and <see cref="IMessageLoggingService"/>
/// to be registered in the DI container. Business logic is delegated to these services
/// so that it can be tested independently of Temporal infrastructure.
/// </remarks>
public sealed class IntegrationActivities
{
    private readonly IMessageValidationService _validation;
    private readonly IMessageLoggingService _logging;

    public IntegrationActivities(
        IMessageValidationService validation,
        IMessageLoggingService logging)
    {
        _validation = validation;
        _logging = logging;
    }

    /// <summary>
    /// Validates that a message payload conforms to the expected schema.
    /// </summary>
    [Activity]
    public async Task<MessageValidationResult> ValidateMessageAsync(
        string messageType, string payloadJson)
    {
        return await _validation.ValidateAsync(messageType, payloadJson);
    }

    /// <summary>
    /// Logs a processing stage for observability / lifecycle tracking.
    /// </summary>
    [Activity]
    public async Task LogProcessingStageAsync(
        Guid messageId, string messageType, string stage)
    {
        await _logging.LogAsync(messageId, messageType, stage);
    }
}
