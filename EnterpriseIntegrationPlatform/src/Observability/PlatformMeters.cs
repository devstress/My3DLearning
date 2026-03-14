using System.Diagnostics.Metrics;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Platform-wide metric instruments for tracking message processing behaviour.
/// All instruments are created from <see cref="DiagnosticsConfig.Meter"/> so they
/// are automatically exported by the configured OpenTelemetry pipeline.
/// </summary>
public static class PlatformMeters
{
    /// <summary>Total number of messages received by the platform.</summary>
    public static readonly Counter<long> MessagesReceived =
        DiagnosticsConfig.Meter.CreateCounter<long>(
            "eip.messages.received",
            unit: "{message}",
            description: "Total number of messages received by the platform.");

    /// <summary>Total number of messages processed successfully.</summary>
    public static readonly Counter<long> MessagesProcessed =
        DiagnosticsConfig.Meter.CreateCounter<long>(
            "eip.messages.processed",
            unit: "{message}",
            description: "Total number of messages processed successfully.");

    /// <summary>Total number of messages that failed processing.</summary>
    public static readonly Counter<long> MessagesFailed =
        DiagnosticsConfig.Meter.CreateCounter<long>(
            "eip.messages.failed",
            unit: "{message}",
            description: "Total number of messages that failed processing.");

    /// <summary>Total number of messages sent to the dead-letter store.</summary>
    public static readonly Counter<long> MessagesDeadLettered =
        DiagnosticsConfig.Meter.CreateCounter<long>(
            "eip.messages.dead_lettered",
            unit: "{message}",
            description: "Total number of messages sent to the dead-letter store.");

    /// <summary>Total number of message retry attempts.</summary>
    public static readonly Counter<long> MessagesRetried =
        DiagnosticsConfig.Meter.CreateCounter<long>(
            "eip.messages.retried",
            unit: "{message}",
            description: "Total number of message retry attempts.");

    /// <summary>Duration of end-to-end message processing in milliseconds.</summary>
    public static readonly Histogram<double> ProcessingDuration =
        DiagnosticsConfig.Meter.CreateHistogram<double>(
            "eip.messages.processing_duration",
            unit: "ms",
            description: "Duration of end-to-end message processing in milliseconds.");

    /// <summary>Number of messages currently in-flight.</summary>
    public static readonly UpDownCounter<long> MessagesInFlight =
        DiagnosticsConfig.Meter.CreateUpDownCounter<long>(
            "eip.messages.in_flight",
            unit: "{message}",
            description: "Number of messages currently in-flight.");

    /// <summary>
    /// Records the receipt of a message, incrementing the received counter and in-flight gauge.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="source">The originating source of the message.</param>
    public static void RecordReceived(string messageType, string source)
    {
        MessagesReceived.Add(1,
            new KeyValuePair<string, object?>("eip.message.type", messageType),
            new KeyValuePair<string, object?>("eip.message.source", source));
        MessagesInFlight.Add(1);
    }

    /// <summary>
    /// Records successful processing of a message, updating counters and recording duration.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="durationMs">Processing duration in milliseconds.</param>
    public static void RecordProcessed(string messageType, double durationMs)
    {
        MessagesProcessed.Add(1,
            new KeyValuePair<string, object?>("eip.message.type", messageType));
        MessagesInFlight.Add(-1);
        ProcessingDuration.Record(durationMs,
            new KeyValuePair<string, object?>("eip.message.type", messageType));
    }

    /// <summary>
    /// Records a failed message processing attempt.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    public static void RecordFailed(string messageType)
    {
        MessagesFailed.Add(1,
            new KeyValuePair<string, object?>("eip.message.type", messageType));
        MessagesInFlight.Add(-1);
    }

    /// <summary>
    /// Records a message being sent to the dead-letter store.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    public static void RecordDeadLettered(string messageType)
    {
        MessagesDeadLettered.Add(1,
            new KeyValuePair<string, object?>("eip.message.type", messageType));
    }

    /// <summary>
    /// Records a message retry attempt.
    /// </summary>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="retryCount">Current retry attempt number.</param>
    public static void RecordRetry(string messageType, int retryCount)
    {
        MessagesRetried.Add(1,
            new KeyValuePair<string, object?>("eip.message.type", messageType),
            new KeyValuePair<string, object?>("eip.retry.count", retryCount));
    }
}
