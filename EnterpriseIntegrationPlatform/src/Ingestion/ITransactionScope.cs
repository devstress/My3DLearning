using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Represents a transactional scope in which publish operations can be executed atomically.
/// Messages published via this scope are either all committed or all rolled back.
/// </summary>
public interface ITransactionScope
{
    /// <summary>
    /// Publishes a message within the current transaction scope.
    /// The message is not visible to consumers until the transaction is committed.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope to publish.</param>
    /// <param name="topic">Target topic or subject name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default);
}
