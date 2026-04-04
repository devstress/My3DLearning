namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Configuration options for the <see cref="ControlBusPublisher"/>.
/// </summary>
public sealed class ControlBusOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ControlBus";

    /// <summary>
    /// The topic used for publishing and subscribing to control messages.
    /// Default is <c>"eip.control-bus"</c>.
    /// </summary>
    public string ControlTopic { get; set; } = "eip.control-bus";

    /// <summary>
    /// The consumer group for control bus subscribers.
    /// Default is <c>"control-bus-consumers"</c>.
    /// </summary>
    public string ConsumerGroup { get; set; } = "control-bus-consumers";

    /// <summary>
    /// The source identifier used when creating control command envelopes.
    /// Default is <c>"ControlBus"</c>.
    /// </summary>
    public string Source { get; set; } = "ControlBus";
}
