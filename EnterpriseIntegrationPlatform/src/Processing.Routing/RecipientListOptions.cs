namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Configuration options for the <see cref="RecipientListRouter"/>.
/// Bound from the <c>RecipientList</c> section of application configuration.
/// </summary>
public sealed class RecipientListOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "RecipientList";

    /// <summary>
    /// Rules that determine which destinations receive the message.
    /// ALL matching rules contribute their destinations; duplicates are removed.
    /// </summary>
    public IReadOnlyList<RecipientListRule> Rules { get; init; } = [];

    /// <summary>
    /// The metadata key whose value (comma-separated) is used to resolve additional
    /// destinations. For example, if set to <c>recipients</c>, the envelope metadata
    /// key <c>recipients</c> value <c>"topic-a,topic-b"</c> adds those two destinations.
    /// When <see langword="null"/> or empty, metadata-based resolution is disabled.
    /// </summary>
    public string? MetadataRecipientsKey { get; init; }
}
