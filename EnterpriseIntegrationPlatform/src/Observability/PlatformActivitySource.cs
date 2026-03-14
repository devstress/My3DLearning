using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Provides factory methods for creating <see cref="Activity"/> spans
/// that are consistently tagged with platform-specific attributes.
/// Use this class instead of calling <see cref="DiagnosticsConfig.ActivitySource"/>
/// directly to ensure uniform tagging across all services.
/// </summary>
public static class PlatformActivitySource
{
    /// <summary>Tag key for the message identifier.</summary>
    public const string TagMessageId = "eip.message.id";

    /// <summary>Tag key for the correlation identifier.</summary>
    public const string TagCorrelationId = "eip.message.correlation_id";

    /// <summary>Tag key for the causation identifier.</summary>
    public const string TagCausationId = "eip.message.causation_id";

    /// <summary>Tag key for the message type.</summary>
    public const string TagMessageType = "eip.message.type";

    /// <summary>Tag key for the originating source.</summary>
    public const string TagSource = "eip.message.source";

    /// <summary>Tag key for message priority.</summary>
    public const string TagPriority = "eip.message.priority";

    /// <summary>Tag key for the processing stage.</summary>
    public const string TagStage = "eip.processing.stage";

    /// <summary>Tag key for the delivery status.</summary>
    public const string TagDeliveryStatus = "eip.delivery.status";

    /// <summary>
    /// Starts a new <see cref="Activity"/> for a named processing stage.
    /// Returns <c>null</c> when no listener is attached.
    /// </summary>
    /// <param name="stageName">Logical name of the processing stage (e.g. "Ingestion", "Routing").</param>
    /// <param name="kind">The <see cref="ActivityKind"/> for this span.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? StartActivity(string stageName, ActivityKind kind = ActivityKind.Internal)
    {
        return DiagnosticsConfig.ActivitySource.StartActivity(stageName, kind);
    }

    /// <summary>
    /// Starts a new <see cref="Activity"/> for processing an <see cref="IntegrationEnvelope{T}"/>
    /// and automatically tags the span with the envelope's metadata.
    /// </summary>
    /// <typeparam name="T">Payload type of the envelope.</typeparam>
    /// <param name="stageName">Logical name of the processing stage.</param>
    /// <param name="envelope">The message envelope being processed.</param>
    /// <param name="kind">The <see cref="ActivityKind"/> for this span.</param>
    /// <returns>A started <see cref="Activity"/> or <c>null</c>.</returns>
    public static Activity? StartActivity<T>(
        string stageName,
        IntegrationEnvelope<T> envelope,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = DiagnosticsConfig.ActivitySource.StartActivity(stageName, kind);
        if (activity is not null)
        {
            TraceEnricher.Enrich(activity, envelope);
        }

        return activity;
    }
}
