using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Replay;

public sealed class InMemoryMessageReplayStore : IMessageReplayStore
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(IntegrationEnvelope<object> Envelope, DateTimeOffset StoredAt)>> _store = new();

    public Task StoreForReplayAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(envelope.Payload);
        var deserializedPayload = JsonSerializer.Deserialize<object>(json) ?? new object();
        var objectEnvelope = new IntegrationEnvelope<object>
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            Timestamp = envelope.Timestamp,
            Source = envelope.Source,
            MessageType = envelope.MessageType,
            SchemaVersion = envelope.SchemaVersion,
            Priority = envelope.Priority,
            Payload = deserializedPayload,
            Metadata = envelope.Metadata
        };

        var queue = _store.GetOrAdd(topic, _ => new ConcurrentQueue<(IntegrationEnvelope<object>, DateTimeOffset)>());
        queue.Enqueue((objectEnvelope, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<IntegrationEnvelope<object>> GetMessagesForReplayAsync(
        string topic,
        ReplayFilter filter,
        int maxMessages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_store.TryGetValue(topic, out var queue))
            yield break;

        var count = 0;
        foreach (var (envelope, _) in queue)
        {
            ct.ThrowIfCancellationRequested();
            if (count >= maxMessages)
                yield break;

            if (filter.CorrelationId.HasValue && envelope.CorrelationId != filter.CorrelationId.Value)
                continue;
            if (!string.IsNullOrEmpty(filter.MessageType) && envelope.MessageType != filter.MessageType)
                continue;
            if (filter.FromTimestamp.HasValue && envelope.Timestamp < filter.FromTimestamp.Value)
                continue;
            if (filter.ToTimestamp.HasValue && envelope.Timestamp > filter.ToTimestamp.Value)
                continue;

            yield return envelope;
            count++;
        }
        await Task.CompletedTask;
    }
}
