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
    /// Root element name used as the canonical wrapper property when converting
    /// non-JSON formats (XML, CSV) to JSON. During XML→JSON conversion this becomes
    /// the top-level JSON property name wrapping the converted document. During
    /// CSV→JSON conversion it names the array property.
    /// Defaults to <c>Root</c>.
    /// </summary>
    public string XmlRootName { get; init; } = "Root";
}
