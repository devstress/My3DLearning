using System.Collections.Concurrent;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Idempotent Receiver — ensures that a message is processed at most once,
/// even if it is delivered multiple times. Uses message ID deduplication.
/// Equivalent to BizTalk idempotent processing via BAM or custom pipeline components.
/// </summary>
public interface IIdempotentReceiver<T>
{
    /// <summary>
    /// Processes the message only if it has not been seen before.
    /// Returns true if the message was processed; false if it was a duplicate.
    /// </summary>
    Task<bool> TryProcessAsync(
        IntegrationEnvelope<T> envelope,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> handler,
        CancellationToken ct = default);
}

/// <summary>
/// In-memory idempotent receiver using a concurrent set of seen message IDs.
/// </summary>
public sealed class IdempotentReceiver<T> : IIdempotentReceiver<T>
{
    private readonly ConcurrentDictionary<Guid, byte> _seen = new();

    /// <summary>Number of unique messages processed.</summary>
    public int ProcessedCount => _seen.Count;

    /// <inheritdoc />
    public async Task<bool> TryProcessAsync(
        IntegrationEnvelope<T> envelope,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        if (!_seen.TryAdd(envelope.MessageId, 0))
            return false; // Duplicate

        await handler(envelope, ct);
        return true;
    }
}
