namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Isolated observability storage for querying message lifecycle events.
/// This is <b>separate</b> from <see cref="IMessageStateStore"/> which is
/// production-only message/integration storage.
/// <para>
/// Implementations may be in-memory (for development), or backed by
/// Elasticsearch, Loki, Seq, or another log-aggregation backend for production.
/// Prometheus handles metrics storage; this interface handles event-level queries.
/// </para>
/// </summary>
public interface IObservabilityEventLog
{
    /// <summary>Records an observability event.</summary>
    Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all lifecycle events for a given business key
    /// (e.g. order number, shipment ID), ordered by timestamp ascending.
    /// </summary>
    Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all lifecycle events for a given correlation identifier,
    /// ordered by timestamp ascending.
    /// </summary>
    Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
