using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Formalizes the Message Store Enterprise Integration Pattern as a unified
/// query interface over the platform's persistent message storage.
/// </summary>
/// <remarks>
/// <para>
/// The Message Store captures messages as they flow through the system,
/// enabling inspection, audit, and replay. This interface wraps the
/// underlying <c>Storage.Cassandra.IMessageRepository</c> and adds
/// system-management query capabilities.
/// </para>
/// </remarks>
public interface IMessageStore
{
    /// <summary>
    /// Retrieves the full message trail for a correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of message records.</returns>
    Task<IReadOnlyList<MessageStoreEntry>> GetTrailAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single message by its unique identifier.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message entry, or <c>null</c> if not found.</returns>
    Task<MessageStoreEntry?> GetByIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of faults recorded for a correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fault count.</returns>
    Task<int> GetFaultCountAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
