using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

public interface IDeadLetterPublisher<T>
{
    Task PublishAsync(
        IntegrationEnvelope<T> envelope,
        DeadLetterReason reason,
        string errorMessage,
        int attemptCount,
        CancellationToken ct);
}
