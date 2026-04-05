namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Enterprise Integration Pattern — Normalizer.
/// Detects the format of an incoming message (JSON, XML, CSV) and converts
/// it to the canonical JSON representation used throughout the platform.
/// </summary>
/// <remarks>
/// The Normalizer pattern routes incoming messages through the correct translation
/// logic so that downstream consumers only ever see a single canonical format.
/// In this platform the canonical model is <c>IntegrationEnvelope&lt;T&gt;</c>,
/// and the canonical payload format is JSON.
/// </remarks>
public interface INormalizer
{
    /// <summary>
    /// Normalizes the <paramref name="payload"/> to canonical JSON format.
    /// </summary>
    /// <param name="payload">The raw payload in any supported format.</param>
    /// <param name="contentType">
    /// MIME-style content type of the payload (e.g. <c>application/json</c>,
    /// <c>application/xml</c>, <c>text/csv</c>).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="NormalizationResult"/> containing the canonical JSON payload.</returns>
    Task<NormalizationResult> NormalizeAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
