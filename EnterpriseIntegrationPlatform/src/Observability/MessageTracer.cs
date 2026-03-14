using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// High-level API for tracing the lifecycle of an <see cref="IntegrationEnvelope{T}"/>
/// as it flows through the platform pipeline (ingestion → routing → transformation → delivery).
/// Combines <see cref="PlatformActivitySource"/>, <see cref="PlatformMeters"/>,
/// <see cref="TraceEnricher"/>, and <see cref="CorrelationPropagator"/> into
/// simple stage-oriented methods.
/// </summary>
public static class MessageTracer
{
    /// <summary>Well-known stage name for message ingestion.</summary>
    public const string StageIngestion = "Ingestion";

    /// <summary>Well-known stage name for content-based routing.</summary>
    public const string StageRouting = "Routing";

    /// <summary>Well-known stage name for message transformation.</summary>
    public const string StageTransformation = "Transformation";

    /// <summary>Well-known stage name for message delivery.</summary>
    public const string StageDelivery = "Delivery";

    /// <summary>
    /// Starts a trace span for message ingestion and records the receipt metric.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope being ingested.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? TraceIngestion<T>(IntegrationEnvelope<T> envelope)
    {
        PlatformMeters.RecordReceived(envelope.MessageType, envelope.Source);
        var activity = PlatformActivitySource.StartActivity(StageIngestion, envelope, ActivityKind.Consumer);
        if (activity is not null)
        {
            TraceEnricher.SetStage(activity, StageIngestion);
            TraceEnricher.SetDeliveryStatus(activity, DeliveryStatus.InFlight);
        }

        return activity;
    }

    /// <summary>
    /// Starts a trace span for message routing.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope being routed.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? TraceRouting<T>(IntegrationEnvelope<T> envelope)
    {
        var activity = PlatformActivitySource.StartActivity(StageRouting, envelope);
        if (activity is not null)
        {
            TraceEnricher.SetStage(activity, StageRouting);
        }

        return activity;
    }

    /// <summary>
    /// Starts a trace span for message transformation.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope being transformed.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? TraceTransformation<T>(IntegrationEnvelope<T> envelope)
    {
        var activity = PlatformActivitySource.StartActivity(StageTransformation, envelope);
        if (activity is not null)
        {
            TraceEnricher.SetStage(activity, StageTransformation);
        }

        return activity;
    }

    /// <summary>
    /// Starts a trace span for message delivery.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope being delivered.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? TraceDelivery<T>(IntegrationEnvelope<T> envelope)
    {
        var activity = PlatformActivitySource.StartActivity(StageDelivery, envelope);
        if (activity is not null)
        {
            TraceEnricher.SetStage(activity, StageDelivery);
        }

        return activity;
    }

    /// <summary>
    /// Marks a processing stage as completed successfully and records the metric.
    /// </summary>
    /// <param name="activity">The activity span to complete. May be <c>null</c>.</param>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="durationMs">Processing duration in milliseconds.</param>
    public static void CompleteSuccess(Activity? activity, string messageType, double durationMs)
    {
        PlatformMeters.RecordProcessed(messageType, durationMs);
        if (activity is not null)
        {
            TraceEnricher.SetDeliveryStatus(activity, DeliveryStatus.Delivered);
            activity.SetStatus(ActivityStatusCode.Ok);
        }
    }

    /// <summary>
    /// Marks a processing stage as failed and records the failure metric and exception.
    /// </summary>
    /// <param name="activity">The activity span to annotate. May be <c>null</c>.</param>
    /// <param name="messageType">The logical message type.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public static void CompleteFailed(Activity? activity, string messageType, Exception exception)
    {
        PlatformMeters.RecordFailed(messageType);
        if (activity is not null)
        {
            TraceEnricher.SetDeliveryStatus(activity, DeliveryStatus.Failed);
            TraceEnricher.RecordException(activity, exception);
        }
    }
}
