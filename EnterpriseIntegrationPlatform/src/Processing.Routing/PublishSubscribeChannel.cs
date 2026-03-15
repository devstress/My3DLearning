using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Publish-Subscribe Channel — broadcasts a message to all registered
/// subscribers. Each subscriber receives its own copy of the message.
/// Equivalent to BizTalk MessageBox subscription model where multiple
/// Send Ports and Orchestrations subscribe to the same message type.
/// </summary>
public interface IPublishSubscribeChannel<T>
{
    /// <summary>Subscribes a handler to receive published messages.</summary>
    IDisposable Subscribe(Func<IntegrationEnvelope<T>, CancellationToken, Task> handler);

    /// <summary>Publishes a message to all current subscribers.</summary>
    Task PublishAsync(IntegrationEnvelope<T> envelope, CancellationToken ct = default);
}

/// <summary>
/// In-memory publish-subscribe channel.
/// </summary>
public sealed class PublishSubscribeChannel<T> : IPublishSubscribeChannel<T>
{
    private readonly List<Func<IntegrationEnvelope<T>, CancellationToken, Task>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>Number of active subscribers.</summary>
    public int SubscriberCount { get { lock (_lock) return _subscribers.Count; } }

    /// <inheritdoc />
    public IDisposable Subscribe(Func<IntegrationEnvelope<T>, CancellationToken, Task> handler)
    {
        lock (_lock) _subscribers.Add(handler);
        return new Unsubscriber(() => { lock (_lock) _subscribers.Remove(handler); });
    }

    /// <inheritdoc />
    public async Task PublishAsync(IntegrationEnvelope<T> envelope, CancellationToken ct = default)
    {
        List<Func<IntegrationEnvelope<T>, CancellationToken, Task>> snapshot;
        lock (_lock) snapshot = _subscribers.ToList();

        var tasks = snapshot.Select(s => s(envelope, ct));
        await Task.WhenAll(tasks);
    }

    private sealed class Unsubscriber(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
