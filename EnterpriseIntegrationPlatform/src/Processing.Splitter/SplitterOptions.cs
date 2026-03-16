namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// Configuration options for <see cref="MessageSplitter{T}"/>.
/// Bind from the <c>MessageSplitter</c> configuration section.
/// </summary>
public sealed class SplitterOptions
{
    /// <summary>
    /// Topic to which the split envelopes are published.
    /// Must be set; an empty value causes <see cref="MessageSplitter{T}"/> to
    /// throw <see cref="InvalidOperationException"/> at split time.
    /// </summary>
    public string TargetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Message type to assign to the split envelopes.
    /// When <see langword="null"/> or empty, the source envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.MessageType"/> is preserved.
    /// </summary>
    public string? TargetMessageType { get; set; }

    /// <summary>
    /// Source identifier to assign to the split envelopes.
    /// When <see langword="null"/> or empty, the source envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.Source"/> is preserved.
    /// </summary>
    public string? TargetSource { get; set; }

    /// <summary>
    /// The JSON property name within the payload that contains the array to split.
    /// Used by <see cref="JsonArraySplitStrategy"/> when the payload is a JSON object
    /// with an array property rather than a top-level array.
    /// When <see langword="null"/> or empty, the strategy expects the payload to be a
    /// top-level JSON array.
    /// </summary>
    public string? ArrayPropertyName { get; set; }
}
