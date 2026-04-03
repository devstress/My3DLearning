namespace EnterpriseIntegrationPlatform.Connectors;

/// <summary>
/// Transport-specific send options passed to <see cref="IConnector.SendAsync{T}"/>.
/// </summary>
/// <remarks>
/// This is the base class. Each transport may define a derived type with
/// transport-specific properties, but the unified interface accepts this base
/// to keep the API generic.
/// </remarks>
public record ConnectorSendOptions
{
    /// <summary>
    /// Target destination — interpretation depends on the connector type:
    /// <list type="bullet">
    ///   <item><description>HTTP: relative URL path</description></item>
    ///   <item><description>SFTP: remote file name</description></item>
    ///   <item><description>Email: recipient address</description></item>
    ///   <item><description>File: ignored (uses configured output path)</description></item>
    /// </list>
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// Optional additional metadata for the send operation.
    /// Keys and values are transport-specific.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();
}
