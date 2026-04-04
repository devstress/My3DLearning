using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Receives messages from a single channel and distributes them to specific
/// handlers based on message type. Implements the Message Dispatcher Enterprise
/// Integration Pattern — a single-channel multiplexer that decouples the
/// consumption of messages from their processing.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Dispatches the given <paramref name="envelope"/> to the handler registered
    /// for its <see cref="IntegrationEnvelope{T}.MessageType"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DispatchResult"/> describing the outcome.</returns>
    Task<DispatchResult> DispatchAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a handler for the specified <paramref name="messageType"/>.
    /// If a handler for the same message type is already registered it is replaced.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="messageType">
    /// The logical message type name that this handler should process.
    /// </param>
    /// <param name="handler">
    /// Async callback invoked when a message of the given type is dispatched.
    /// </param>
    void Register<T>(string messageType, Func<IntegrationEnvelope<T>, CancellationToken, Task> handler);

    /// <summary>
    /// Removes the handler registered for <paramref name="messageType"/>.
    /// </summary>
    /// <param name="messageType">Message type to unregister.</param>
    /// <returns><c>true</c> if a handler was removed; otherwise <c>false</c>.</returns>
    bool Unregister(string messageType);

    /// <summary>
    /// Returns the message types that currently have registered handlers.
    /// </summary>
    IReadOnlyCollection<string> RegisteredTypes { get; }
}
