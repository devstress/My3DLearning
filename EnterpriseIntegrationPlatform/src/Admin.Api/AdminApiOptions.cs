namespace EnterpriseIntegrationPlatform.Admin.Api;

/// <summary>
/// Configuration options for the Admin API.
/// Bound from the <c>AdminApi</c> section of application configuration.
/// </summary>
public sealed class AdminApiOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "AdminApi";

    /// <summary>
    /// API keys that are authorised to access every Admin API endpoint.
    /// At least one key must be configured in production.
    /// Keys are compared using ordinal (case-sensitive) matching.
    /// </summary>
    public IReadOnlyList<string> ApiKeys { get; init; } = [];

    /// <summary>
    /// Maximum number of requests per API key (or per client IP when no key is
    /// supplied) within a one-minute fixed window.
    /// Requests that exceed this limit receive HTTP 429.
    /// </summary>
    public int RateLimitPerMinute { get; init; } = 60;
}
