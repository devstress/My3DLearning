namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Configuration options for the Content-Based Router.
/// Bound from the <c>ContentBasedRouter</c> section of application configuration.
/// </summary>
public sealed class RouterOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "ContentBasedRouter";

    /// <summary>
    /// Ordered list of routing rules.
    /// Rules are evaluated in ascending <see cref="RoutingRule.Priority"/> order.
    /// </summary>
    public IReadOnlyList<RoutingRule> Rules { get; init; } = [];

    /// <summary>
    /// The topic or subject used when no rule matches the message.
    /// When <see langword="null"/> or empty, an unmatched message causes a
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    public string? DefaultTopic { get; init; }
}
