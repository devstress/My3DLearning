namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Enterprise Integration Pattern — Content Enricher.
/// Augments a message payload with additional data obtained from an external source
/// (e.g. HTTP lookup, database query, cache). The enricher merges the external data
/// into the original payload without losing existing fields.
/// </summary>
public interface IContentEnricher
{
    /// <summary>
    /// Enriches the <paramref name="payload"/> by fetching supplementary data from the
    /// configured external source and merging it into the payload.
    /// </summary>
    /// <param name="payload">The original JSON payload to enrich.</param>
    /// <param name="correlationId">Correlation identifier for tracing and logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched JSON payload.</returns>
    Task<string> EnrichAsync(
        string payload,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
