using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Enriches <see cref="Activity"/> spans with metadata from
/// <see cref="IntegrationEnvelope{T}"/> instances so that every trace
/// carries the full message context for debugging and analysis.
/// </summary>
public static class TraceEnricher
{
    /// <summary>
    /// Applies standard platform tags from the given envelope to the activity.
    /// </summary>
    /// <typeparam name="T">Payload type of the envelope.</typeparam>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="envelope">The message envelope whose metadata is applied.</param>
    public static void Enrich<T>(Activity activity, IntegrationEnvelope<T> envelope)
    {
        activity.SetTag(PlatformActivitySource.TagMessageId, envelope.MessageId.ToString());
        activity.SetTag(PlatformActivitySource.TagCorrelationId, envelope.CorrelationId.ToString());
        activity.SetTag(PlatformActivitySource.TagMessageType, envelope.MessageType);
        activity.SetTag(PlatformActivitySource.TagSource, envelope.Source);
        activity.SetTag(PlatformActivitySource.TagPriority, envelope.Priority.ToString());

        if (envelope.CausationId.HasValue)
        {
            activity.SetTag(PlatformActivitySource.TagCausationId, envelope.CausationId.Value.ToString());
        }
    }

    /// <summary>
    /// Applies a <see cref="DeliveryStatus"/> tag to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="status">The current delivery status.</param>
    public static void SetDeliveryStatus(Activity activity, DeliveryStatus status)
    {
        activity.SetTag(PlatformActivitySource.TagDeliveryStatus, status.ToString());
    }

    /// <summary>
    /// Applies a processing stage tag to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="stage">The logical processing stage name.</param>
    public static void SetStage(Activity activity, string stage)
    {
        activity.SetTag(PlatformActivitySource.TagStage, stage);
    }

    /// <summary>
    /// Records an exception on the activity and sets the status to <see cref="ActivityStatusCode.Error"/>.
    /// </summary>
    /// <param name="activity">The activity to annotate.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void RecordException(Activity activity, Exception exception)
    {
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace ?? string.Empty },
        }));
    }
}
