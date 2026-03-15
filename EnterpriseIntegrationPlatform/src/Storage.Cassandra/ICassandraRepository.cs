using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Storage.Cassandra;

/// <summary>
/// Generic repository for persisting and querying <see cref="IntegrationEnvelope{T}"/>
/// messages in Cassandra.
/// </summary>
public interface ICassandraRepository<T>
{
    /// <summary>Persists a message envelope.</summary>
    Task SaveAsync(IntegrationEnvelope<T> envelope, CancellationToken ct = default);

    /// <summary>Retrieves a message by its unique message ID.</summary>
    Task<IntegrationEnvelope<T>?> GetByIdAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>Retrieves all messages sharing a correlation ID.</summary>
    Task<IReadOnlyList<IntegrationEnvelope<T>>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken ct = default);

    /// <summary>Deletes a message by its unique message ID.</summary>
    Task<bool> DeleteAsync(Guid messageId, CancellationToken ct = default);
}
