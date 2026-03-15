namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Configuration options for the demo integration pipeline.
/// Bound from the <c>Pipeline</c> configuration section.
/// </summary>
public sealed class PipelineOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Pipeline";

    /// <summary>NATS server URL (e.g. <c>nats://localhost:15222</c>).</summary>
    public string NatsUrl { get; set; } = "nats://localhost:15222";

    /// <summary>NATS JetStream subject for inbound messages.</summary>
    public string InboundSubject { get; set; } = "integration.inbound";

    /// <summary>NATS JetStream subject for Ack notifications (successful processing).</summary>
    public string AckSubject { get; set; } = "integration.ack";

    /// <summary>NATS JetStream subject for Nack notifications (failed processing).</summary>
    public string NackSubject { get; set; } = "integration.nack";

    /// <summary>Consumer group name for the inbound NATS subscription.</summary>
    public string ConsumerGroup { get; set; } = "demo-pipeline";

    /// <summary>Temporal server gRPC address (e.g. <c>localhost:15233</c>).</summary>
    public string TemporalServerAddress { get; set; } = "localhost:15233";

    /// <summary>Temporal namespace for workflow execution.</summary>
    public string TemporalNamespace { get; set; } = "default";

    /// <summary>Temporal task queue that the workflow worker polls.</summary>
    public string TemporalTaskQueue { get; set; } = "integration-workflows";

    /// <summary>
    /// Maximum time to wait for a Temporal workflow to complete before
    /// treating the message as failed.
    /// </summary>
    public TimeSpan WorkflowTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
