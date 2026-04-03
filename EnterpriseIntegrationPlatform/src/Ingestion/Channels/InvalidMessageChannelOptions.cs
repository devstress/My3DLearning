namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Configuration options for <see cref="InvalidMessageChannel"/>.
/// </summary>
public sealed class InvalidMessageChannelOptions
{
    /// <summary>
    /// Topic name for routing invalid/malformed messages.
    /// Default is <c>"invalid-messages"</c>.
    /// </summary>
    public string InvalidMessageTopic { get; set; } = "invalid-messages";

    /// <summary>
    /// Source name stamped on envelopes routed to the invalid channel.
    /// Default is <c>"InvalidMessageChannel"</c>.
    /// </summary>
    public string Source { get; set; } = "InvalidMessageChannel";
}
