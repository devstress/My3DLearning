namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Configuration options for <see cref="ContentEnricher"/>.
/// </summary>
public sealed class ContentEnricherOptions
{
    /// <summary>
    /// The URL template for the external enrichment source. Use <c>{key}</c> as
    /// a placeholder that will be replaced with the value extracted from the payload
    /// via <see cref="LookupKeyPath"/>.
    /// </summary>
    /// <example><c>https://api.example.com/customers/{key}</c></example>
    public required string EndpointUrlTemplate { get; init; }

    /// <summary>
    /// Dot-separated JSON path pointing to the lookup key in the source payload
    /// (e.g. <c>order.customerId</c>). The extracted value is substituted into
    /// <see cref="EndpointUrlTemplate"/>.
    /// </summary>
    public required string LookupKeyPath { get; init; }

    /// <summary>
    /// Dot-separated JSON path at which the enrichment data will be merged into the
    /// original payload (e.g. <c>customer</c>). If the path already exists, it will
    /// be overwritten.
    /// </summary>
    public required string MergeTargetPath { get; init; }

    /// <summary>
    /// HTTP request timeout for the enrichment call. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// When <see langword="true"/>, enrichment failures (network errors, non-2xx
    /// responses) are treated as non-fatal — the original payload is returned
    /// unchanged. When <see langword="false"/>, exceptions are propagated.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool FallbackOnFailure { get; init; } = true;

    /// <summary>
    /// Optional static fallback value (JSON fragment) to merge when the external
    /// call fails and <see cref="FallbackOnFailure"/> is <see langword="true"/>.
    /// When <see langword="null"/>, no merge occurs on failure.
    /// </summary>
    public string? FallbackValue { get; init; }
}
