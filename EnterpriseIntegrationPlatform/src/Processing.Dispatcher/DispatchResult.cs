namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Describes the outcome of a <see cref="IMessageDispatcher.DispatchAsync{T}"/> call.
/// </summary>
/// <param name="MessageType">The message type that was dispatched.</param>
/// <param name="HandlerFound">Whether a handler was registered for the message type.</param>
/// <param name="Succeeded">Whether the handler completed without throwing.</param>
/// <param name="FailureReason">Description of the failure, if any.</param>
public sealed record DispatchResult(
    string MessageType,
    bool HandlerFound,
    bool Succeeded,
    string? FailureReason = null);
