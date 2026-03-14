using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Propagates correlation identifiers between <see cref="IntegrationEnvelope{T}"/>
/// metadata and the current OpenTelemetry <see cref="Activity"/> context.
/// This ensures trace context flows across service boundaries even when
/// messages are exchanged through non-HTTP transports such as Kafka.
/// </summary>
public static class CorrelationPropagator
{
    /// <summary>
    /// Injects the current <see cref="Activity"/> trace context into the
    /// envelope's <see cref="IntegrationEnvelope{T}.Metadata"/> dictionary.
    /// </summary>
    /// <typeparam name="T">Payload type of the envelope.</typeparam>
    /// <param name="envelope">The envelope to inject trace context into.</param>
    /// <returns>The same envelope with trace headers set in <see cref="IntegrationEnvelope{T}.Metadata"/>.</returns>
    public static IntegrationEnvelope<T> InjectTraceContext<T>(IntegrationEnvelope<T> envelope)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return envelope;
        }

        envelope.Metadata[MessageHeaders.TraceId] = activity.TraceId.ToString();
        envelope.Metadata[MessageHeaders.SpanId] = activity.SpanId.ToString();

        return envelope;
    }

    /// <summary>
    /// Extracts trace context from the envelope's <see cref="IntegrationEnvelope{T}.Metadata"/>
    /// and starts a new <see cref="Activity"/> that is parented to the extracted context.
    /// This restores the distributed trace chain on the consuming side.
    /// </summary>
    /// <typeparam name="T">Payload type of the envelope.</typeparam>
    /// <param name="envelope">The envelope containing trace headers.</param>
    /// <param name="stageName">Logical name for the new activity span.</param>
    /// <param name="kind">The <see cref="ActivityKind"/> for the new span.</param>
    /// <returns>A started <see cref="Activity"/> linked to the upstream context, or <c>null</c>.</returns>
    public static Activity? ExtractAndStart<T>(
        IntegrationEnvelope<T> envelope,
        string stageName,
        ActivityKind kind = ActivityKind.Consumer)
    {
        ActivityContext parentContext = default;

        if (envelope.Metadata.TryGetValue(MessageHeaders.TraceId, out var traceIdStr) &&
            envelope.Metadata.TryGetValue(MessageHeaders.SpanId, out var spanIdStr) &&
            ActivityTraceId.CreateFromString(traceIdStr) is var traceId &&
            ActivitySpanId.CreateFromString(spanIdStr) is var spanId)
        {
            parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
        }

        var activity = DiagnosticsConfig.ActivitySource.StartActivity(
            stageName,
            kind,
            parentContext);

        if (activity is not null)
        {
            TraceEnricher.Enrich(activity, envelope);
        }

        return activity;
    }
}
