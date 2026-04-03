namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Configuration options for <see cref="DatatypeChannel"/>.
/// </summary>
public sealed class DatatypeChannelOptions
{
    /// <summary>
    /// Prefix prepended to the <see cref="Contracts.IntegrationEnvelope{T}.MessageType"/>
    /// when resolving the target topic. Default is <c>"datatype"</c>.
    /// </summary>
    public string TopicPrefix { get; set; } = "datatype";

    /// <summary>
    /// Separator between the prefix and the message type in the resolved topic name.
    /// Default is <c>"."</c>.
    /// </summary>
    public string Separator { get; set; } = ".";
}
