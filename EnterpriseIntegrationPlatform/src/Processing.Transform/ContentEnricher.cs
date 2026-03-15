using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Content Enricher — augments a message with additional data from an
/// external source (database, API, cache). Equivalent to BizTalk
/// orchestration shapes that call external systems to enrich messages
/// before routing.
/// </summary>
public interface IContentEnricher<T>
{
    /// <summary>
    /// Enriches the message by adding data from external sources.
    /// Returns a new envelope with the enriched payload.
    /// </summary>
    Task<IntegrationEnvelope<T>> EnrichAsync(
        IntegrationEnvelope<T> envelope, CancellationToken ct = default);
}

/// <summary>
/// Delegate-based content enricher.
/// </summary>
public sealed class ContentEnricher<T> : IContentEnricher<T>
{
    private readonly Func<T, CancellationToken, Task<T>> _enrichFunc;

    public ContentEnricher(Func<T, CancellationToken, Task<T>> enrichFunc)
    {
        _enrichFunc = enrichFunc;
    }

    /// <inheritdoc />
    public async Task<IntegrationEnvelope<T>> EnrichAsync(
        IntegrationEnvelope<T> envelope, CancellationToken ct = default)
    {
        var enriched = await _enrichFunc(envelope.Payload, ct);

        return IntegrationEnvelope<T>.Create(
            enriched,
            envelope.Source,
            envelope.MessageType,
            envelope.CorrelationId,
            envelope.MessageId);
    }
}
