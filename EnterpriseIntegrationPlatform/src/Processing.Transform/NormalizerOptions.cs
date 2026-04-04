namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Configuration options for <see cref="MessageNormalizer"/>.
/// </summary>
public sealed class NormalizerOptions
{
    /// <summary>
    /// When <see langword="true"/>, the normalizer raises an exception for unknown
    /// content types. When <see langword="false"/>, it attempts best-effort detection
    /// by inspecting the payload content. Defaults to <see langword="true"/>.
    /// </summary>
    public bool StrictContentType { get; init; } = true;

    /// <summary>
    /// The default CSV delimiter character used when parsing CSV payloads.
    /// Defaults to <c>,</c>.
    /// </summary>
    public char CsvDelimiter { get; init; } = ',';

    /// <summary>
    /// When <see langword="true"/>, CSV headers are treated as JSON property names.
    /// When <see langword="false"/>, each row is an array of strings.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool CsvHasHeaders { get; init; } = true;

    /// <summary>
    /// XML root element name for the canonical representation when converting from
    /// formats other than XML. Not used during XML→JSON normalization.
    /// Defaults to <c>Root</c>.
    /// </summary>
    public string XmlRootName { get; init; } = "Root";
}
