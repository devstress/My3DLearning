using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Production implementation of the Message Dispatcher Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The Message Dispatcher receives messages from a single channel and distributes
/// them to specific handlers based on <see cref="IntegrationEnvelope{T}.MessageType"/>.
/// It acts as a multiplexer that decouples message consumption from processing.
/// </para>
/// <para>
/// Handlers are registered dynamically via <see cref="Register{T}"/>. Thread-safe
/// handler registration and dispatch is ensured via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// </remarks>
public sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly ConcurrentDictionary<string, Delegate> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly MessageDispatcherOptions _options;
    private readonly ILogger<MessageDispatcher> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageDispatcher"/>.</summary>
    public MessageDispatcher(
        IOptions<MessageDispatcherOptions> options,
        ILogger<MessageDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredTypes =>
        _handlers.Keys.ToArray();

    /// <inheritdoc />
    public void Register<T>(
        string messageType,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[messageType] = handler;

        _logger.LogDebug("Handler registered for message type '{MessageType}'", messageType);
    }

    /// <inheritdoc />
    public bool Unregister(string messageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        var removed = _handlers.TryRemove(messageType, out _);

        if (removed)
            _logger.LogDebug("Handler unregistered for message type '{MessageType}'", messageType);

        return removed;
    }

    /// <inheritdoc />
    public async Task<DispatchResult> DispatchAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var messageType = envelope.MessageType;

        if (!_handlers.TryGetValue(messageType, out var handlerDelegate))
        {
            _logger.LogWarning(
                "No handler registered for message type '{MessageType}' (MessageId={MessageId})",
                messageType, envelope.MessageId);

            if (_options.ThrowOnUnknownType)
            {
                throw new InvalidOperationException(
                    $"No handler registered for message type '{messageType}' and ThrowOnUnknownType is enabled.");
            }

            return new DispatchResult(messageType, HandlerFound: false, Succeeded: false,
                FailureReason: $"No handler registered for message type '{messageType}'.");
        }

        if (handlerDelegate is not Func<IntegrationEnvelope<T>, CancellationToken, Task> typedHandler)
        {
            _logger.LogError(
                "Handler for message type '{MessageType}' has incompatible type signature (MessageId={MessageId})",
                messageType, envelope.MessageId);

            return new DispatchResult(messageType, HandlerFound: true, Succeeded: false,
                FailureReason: $"Handler for '{messageType}' has incompatible type signature for payload type {typeof(T).Name}.");
        }

        try
        {
            await typedHandler(envelope, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) dispatched successfully",
                envelope.MessageId, messageType);

            return new DispatchResult(messageType, HandlerFound: true, Succeeded: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Let cancellation propagate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Handler for message type '{MessageType}' threw an exception (MessageId={MessageId})",
                messageType, envelope.MessageId);

            return new DispatchResult(messageType, HandlerFound: true, Succeeded: false,
                FailureReason: ex.Message);
        }
    }
}
