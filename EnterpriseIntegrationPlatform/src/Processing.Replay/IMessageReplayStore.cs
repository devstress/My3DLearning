using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Replay;

public interface IMessageReplayStore
{
    Task StoreForReplayAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct);
    IAsyncEnumerable<IntegrationEnvelope<object>> GetMessagesForReplayAsync(
        string topic,
        ReplayFilter filter,
        int maxMessages,
        CancellationToken ct);
}
