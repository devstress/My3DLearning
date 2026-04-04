namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Configuration options for the <see cref="DynamicRouter"/>.
/// Bound from the <c>DynamicRouter</c> section of application configuration.
/// </summary>
public sealed class DynamicRouterOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "DynamicRouter";

    /// <summary>
    /// The envelope field used to look up the routing table entry.
    /// Supported values: <c>MessageType</c>, <c>Source</c>, <c>Priority</c>,
    /// <c>Metadata.{key}</c>. Defaults to <c>MessageType</c>.
    /// </summary>
    public string ConditionField { get; init; } = "MessageType";

    /// <summary>
    /// The topic or subject used when no routing table entry matches the message.
    /// When <see langword="null"/> or empty, an unmatched message causes an
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    public string? FallbackTopic { get; init; }

    /// <summary>
    /// When <see langword="true"/>, condition key comparison is case-insensitive.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool CaseInsensitive { get; init; } = true;
}
