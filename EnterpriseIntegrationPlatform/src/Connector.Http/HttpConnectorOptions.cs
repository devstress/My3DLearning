namespace EnterpriseIntegrationPlatform.Connector.Http;

/// <summary>
/// Configuration options for the HTTP connector.
/// </summary>
public sealed class HttpConnectorOptions
{
    /// <summary>Base URL of the target HTTP service (required).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>HTTP request timeout in seconds. Default is 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum number of retry attempts on transient failures. Default is 3.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay in milliseconds between retry attempts. Default is 1000.</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Seconds before a cached authentication token is considered expired. Default is 300.
    /// </summary>
    public int CacheTokenExpirySeconds { get; set; } = 300;

    /// <summary>Default headers to add to every outgoing request.</summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}
