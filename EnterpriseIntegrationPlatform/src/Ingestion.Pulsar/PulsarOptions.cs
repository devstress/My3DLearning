namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Configuration options for the Apache Pulsar message broker provider.
/// Bound from the <c>Pulsar</c> configuration section via IOptions pattern.
/// </summary>
public sealed class PulsarOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Pulsar";

    /// <summary>Pulsar service URL. Default: <c>pulsar://localhost:6650</c>.</summary>
    public string ServiceUrl { get; set; } = "pulsar://localhost:6650";

    /// <summary>
    /// Operation timeout in milliseconds for Pulsar client operations.
    /// Default: 30000 (30 seconds).
    /// </summary>
    public int OperationTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Keep-alive interval in milliseconds for the Pulsar client connection.
    /// Default: 30000 (30 seconds).
    /// </summary>
    public int KeepAliveIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

        if (!Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "pulsar" && uri.Scheme != "pulsar+ssl"))
        {
            throw new ArgumentException(
                $"ServiceUrl must be a valid pulsar:// or pulsar+ssl:// URI. Got: '{ServiceUrl}'.",
                nameof(ServiceUrl));
        }

        if (OperationTimeoutMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(OperationTimeoutMs), OperationTimeoutMs,
                "OperationTimeoutMs must be positive.");

        if (KeepAliveIntervalMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(KeepAliveIntervalMs), KeepAliveIntervalMs,
                "KeepAliveIntervalMs must be positive.");
    }
}
