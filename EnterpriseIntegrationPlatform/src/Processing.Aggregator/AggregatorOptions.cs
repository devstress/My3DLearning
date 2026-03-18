namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// Configuration options for <see cref="MessageAggregator{TItem,TAggregate}"/>.
/// Bind from the <c>MessageAggregator</c> configuration section.
/// </summary>
public sealed class AggregatorOptions
{
    /// <summary>
    /// Topic to which the aggregate envelope is published when the group is complete.
    /// Must be set; an empty value causes <see cref="MessageAggregator{TItem,TAggregate}"/>
    /// to throw <see cref="InvalidOperationException"/> at aggregation time.
    /// </summary>
    public string TargetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Message type to assign to the aggregate envelope.
    /// When <see langword="null"/> or empty, the first received envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.MessageType"/> is used.
    /// </summary>
    public string? TargetMessageType { get; set; }

    /// <summary>
    /// Source identifier to assign to the aggregate envelope.
    /// When <see langword="null"/> or empty, the first received envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.Source"/> is used.
    /// </summary>
    public string? TargetSource { get; set; }

    /// <summary>
    /// The number of individual messages expected before the group is considered
    /// complete when using <see cref="CountCompletionStrategy{T}"/>.
    /// Must be greater than zero when no custom completion predicate is provided via
    /// <see cref="AggregatorServiceExtensions"/>.
    /// </summary>
    public int ExpectedCount { get; set; }
}
