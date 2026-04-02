namespace EnterpriseIntegrationPlatform.Gateway.Api.Configuration;

/// <summary>
/// Configuration options for the API Gateway.
/// Bind from the <c>Gateway</c> configuration section.
/// </summary>
public sealed class GatewayOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Gateway";

    /// <summary>Base URL for the Admin API downstream service.</summary>
    public string AdminApiBaseUrl { get; set; } = "http://localhost:5200";

    /// <summary>Base URL for the OpenClaw Web downstream service.</summary>
    public string OpenClawBaseUrl { get; set; } = "http://localhost:5100";

    /// <summary>Maximum requests per minute per client IP (fixed window).</summary>
    public int RateLimitPerMinute { get; set; } = 100;

    /// <summary>Maximum requests per minute globally (sliding window).</summary>
    public int GlobalRateLimitPerMinute { get; set; } = 1000;

    /// <summary>When <c>true</c>, HTTPS is required for all requests.</summary>
    public bool RequireHttps { get; set; }

    /// <summary>Allowed CORS origins. Defaults to all origins.</summary>
    public string[] AllowedOrigins { get; set; } = ["*"];
}
