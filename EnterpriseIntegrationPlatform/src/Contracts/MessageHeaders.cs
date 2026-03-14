namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Well-known keys for the <see cref="IntegrationEnvelope{T}.Metadata"/> dictionary.
/// Use these constants when reading or writing header values to ensure consistency
/// across all platform services.
/// </summary>
public static class MessageHeaders
{
    /// <summary>OpenTelemetry W3C trace-id propagated across service boundaries.</summary>
    public const string TraceId = "trace-id";

    /// <summary>OpenTelemetry W3C span-id of the producing span.</summary>
    public const string SpanId = "span-id";

    /// <summary>MIME content-type of the serialised payload, e.g. "application/json".</summary>
    public const string ContentType = "content-type";

    /// <summary>Schema version of the message contract, mirrors <see cref="IntegrationEnvelope{T}.SchemaVersion"/>.</summary>
    public const string SchemaVersion = "schema-version";

    /// <summary>Name of the Kafka topic (or other transport channel) the message was published to.</summary>
    public const string SourceTopic = "source-topic";

    /// <summary>Name of the consumer group that processed the message.</summary>
    public const string ConsumerGroup = "consumer-group";

    /// <summary>ISO-8601 UTC timestamp of the most recent processing attempt.</summary>
    public const string LastAttemptAt = "last-attempt-at";

    /// <summary>Zero-based number of times this message has been retried.</summary>
    public const string RetryCount = "retry-count";
}
