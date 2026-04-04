namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Configuration options for the <see cref="ServiceActivator"/>.
/// </summary>
public sealed class ServiceActivatorOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ServiceActivator";

    /// <summary>
    /// The source identifier to use when creating reply envelopes.
    /// Default is <c>"ServiceActivator"</c>.
    /// </summary>
    public string ReplySource { get; set; } = "ServiceActivator";

    /// <summary>
    /// The message type to use on reply envelopes.
    /// Default is <c>"service-activator.reply"</c>.
    /// </summary>
    public string ReplyMessageType { get; set; } = "service-activator.reply";
}
