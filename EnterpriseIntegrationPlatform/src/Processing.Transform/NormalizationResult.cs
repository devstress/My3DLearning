namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Result of a normalization operation.
/// </summary>
/// <param name="Payload">The normalized payload in canonical JSON format.</param>
/// <param name="OriginalContentType">The content type of the original payload before normalization.</param>
/// <param name="DetectedFormat">The format that was detected and converted (e.g. "JSON", "XML", "CSV").</param>
/// <param name="WasTransformed">
/// <see langword="true"/> if a format conversion occurred;
/// <see langword="false"/> if the payload was already in canonical JSON format.
/// </param>
public sealed record NormalizationResult(
    string Payload,
    string OriginalContentType,
    string DetectedFormat,
    bool WasTransformed);
