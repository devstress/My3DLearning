using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Transactional Client — wraps produce and consume operations in a transactional scope
/// to ensure atomicity. Implements the Transactional Client EIP pattern.
///
/// <para>
/// For brokers that support native transactions (Kafka), the transactional client uses
/// the broker's built-in transaction support. For brokers without native transactions
/// (NATS JetStream, Pulsar), the client provides publish-then-confirm semantics with
/// compensation on failure.
/// </para>
///
/// <para>
/// EIP Pattern: <c>Transactional Client</c> (Chapter 10, p. 484 of Enterprise Integration Patterns).
/// </para>
/// </summary>
public interface ITransactionalClient
{
    /// <summary>
    /// Executes a set of publish operations within a transactional scope.
    /// All operations succeed atomically or are rolled back.
    /// </summary>
    /// <param name="operations">
    /// An async callback that receives a <see cref="ITransactionScope"/> to publish messages within the transaction.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the transaction was committed or rolled back.</returns>
    Task<TransactionResult> ExecuteAsync(
        Func<ITransactionScope, CancellationToken, Task> operations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the underlying broker supports native transactions.
    /// When <c>true</c>, the broker provides atomic commit/rollback. When <c>false</c>,
    /// the client uses publish-then-confirm with compensation.
    /// </summary>
    bool SupportsNativeTransactions { get; }
}
