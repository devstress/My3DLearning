namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Describes the outcome of a <see cref="IConnector.SendAsync{T}"/> invocation.
/// </summary>
public sealed record ConnectorResult
{
    /// <summary>Whether the send operation succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>The connector name that processed the request.</summary>
    public required string ConnectorName { get; init; }

    /// <summary>
    /// Human-readable status message (e.g. "200 OK", "File written to /data/out/msg.json").
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Error detail when <see cref="Success"/> is <see langword="false"/>.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>UTC timestamp when the operation completed.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Creates a success result.</summary>
    public static ConnectorResult Ok(string connectorName, string? statusMessage = null) =>
        new()
        {
            Success = true,
            ConnectorName = connectorName,
            StatusMessage = statusMessage,
        };

    /// <summary>Creates a failure result.</summary>
    public static ConnectorResult Fail(string connectorName, string errorMessage) =>
        new()
        {
            Success = false,
            ConnectorName = connectorName,
            ErrorMessage = errorMessage,
        };
}
