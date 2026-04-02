namespace EnterpriseIntegrationPlatform.Gateway.Api.Configuration;

/// <summary>
/// Defines a single route mapping from an incoming request pattern to a downstream service.
/// </summary>
public sealed class RouteDefinition
{
    /// <summary>The URL pattern to match (e.g. <c>/api/v1/admin</c>).</summary>
    public required string Pattern { get; init; }

    /// <summary>The logical name of the downstream service.</summary>
    public required string DownstreamService { get; init; }

    /// <summary>The path prefix on the downstream service.</summary>
    public required string DownstreamPath { get; init; }

    /// <summary>Whether this route requires authentication. Defaults to <c>true</c>.</summary>
    public bool RequiresAuth { get; init; } = true;

    /// <summary>Optional rate-limit policy name to apply to this route.</summary>
    public string? RateLimitPolicy { get; init; }
}
