namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Defines a single field mapping used by <see cref="JsonFieldMappingTransform"/> to
/// copy or set a value from a source JSON document into a target JSON document.
/// </summary>
public sealed record FieldMapping
{
    /// <summary>
    /// Dot-separated path of the property to read from the source JSON document
    /// (e.g. <c>order.id</c> or <c>customer.address.city</c>).
    /// Ignored when <see cref="StaticValue"/> is set.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Dot-separated path of the property to write in the target JSON document
    /// (e.g. <c>orderId</c> or <c>shippingCity</c>).
    /// Intermediate objects are created automatically when the path has multiple segments.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// When set, this constant string value is written to <see cref="TargetPath"/> in the
    /// target document regardless of the source document contents.
    /// Use this to inject fixed values such as a schema version or system identifier.
    /// </summary>
    public string? StaticValue { get; init; }
}
