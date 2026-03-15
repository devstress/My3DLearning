using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Wire Tap — inspects messages flowing through the system without
/// altering them. Used for debugging, auditing, and monitoring.
/// Equivalent to BizTalk Tracking / MessageBox subscriptions for
/// monitoring without disrupting the message flow.
/// </summary>
public interface IWireTap<T>
{
    /// <summary>Inspects the message (read-only, non-blocking).</summary>
    Task TapAsync(IntegrationEnvelope<T> envelope, CancellationToken ct = default);
}

/// <summary>
/// In-memory wire tap that records tapped messages for inspection.
/// </summary>
public sealed class InMemoryWireTap<T> : IWireTap<T>
{
    private readonly List<IntegrationEnvelope<T>> _tapped = new();
    private readonly object _lock = new();

    /// <summary>All messages that have been tapped.</summary>
    public IReadOnlyList<IntegrationEnvelope<T>> TappedMessages
    {
        get { lock (_lock) return _tapped.ToList(); }
    }

    /// <inheritdoc />
    public Task TapAsync(IntegrationEnvelope<T> envelope, CancellationToken ct = default)
    {
        lock (_lock) _tapped.Add(envelope);
        return Task.CompletedTask;
    }
}
