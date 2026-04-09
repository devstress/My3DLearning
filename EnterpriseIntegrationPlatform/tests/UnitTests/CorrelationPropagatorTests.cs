using System.Diagnostics;
using System.Diagnostics.Metrics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CorrelationPropagatorTests
{
    private ActivityListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown()
    {
        _listener.Dispose();
        Activity.Current = null;
    }

    [Test]
    public void InjectTraceContext_NoCurrentActivity_ReturnsEnvelopeUnchanged()
    {
        Activity.Current = null;
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");

        var result = CorrelationPropagator.InjectTraceContext(envelope);

        Assert.That(result, Is.SameAs(envelope));
        Assert.That(result.Metadata.ContainsKey(MessageHeaders.TraceId), Is.False);
        Assert.That(result.Metadata.ContainsKey(MessageHeaders.SpanId), Is.False);
    }

    [Test]
    public void InjectTraceContext_WithCurrentActivity_SetsTraceAndSpanHeaders()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test-inject")!;
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");

        var result = CorrelationPropagator.InjectTraceContext(envelope);

        Assert.That(result.Metadata[MessageHeaders.TraceId], Is.EqualTo(activity.TraceId.ToString()));
        Assert.That(result.Metadata[MessageHeaders.SpanId], Is.EqualTo(activity.SpanId.ToString()));
    }

    [Test]
    public void ExtractAndStart_WithTraceHeaders_CreatesChildActivity()
    {
        using var parent = DiagnosticsConfig.ActivitySource.StartActivity("parent")!;
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");
        envelope.Metadata[MessageHeaders.TraceId] = parent.TraceId.ToString();
        envelope.Metadata[MessageHeaders.SpanId] = parent.SpanId.ToString();
        parent.Stop();
        Activity.Current = null;

        using var child = CorrelationPropagator.ExtractAndStart(envelope, "child-stage");

        Assert.That(child, Is.Not.Null);
        Assert.That(child!.ParentId, Does.Contain(parent.TraceId.ToString()));
        Assert.That(child.OperationName, Is.EqualTo("child-stage"));
    }

    [Test]
    public void ExtractAndStart_WithoutTraceHeaders_StartsActivityWithoutParent()
    {
        Activity.Current = null;
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");

        using var activity = CorrelationPropagator.ExtractAndStart(envelope, "no-parent-stage");

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.OperationName, Is.EqualTo("no-parent-stage"));
    }

    [Test]
    public void ExtractAndStart_DefaultKind_IsConsumer()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");

        using var activity = CorrelationPropagator.ExtractAndStart(envelope, "consumer-stage");

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Kind, Is.EqualTo(ActivityKind.Consumer));
    }

    [Test]
    public void ExtractAndStart_ExplicitKind_IsRespected()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "TestMsg");

        using var activity = CorrelationPropagator.ExtractAndStart(envelope, "producer-stage", ActivityKind.Producer);

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Kind, Is.EqualTo(ActivityKind.Producer));
    }
}

[TestFixture]
public class PlatformMetersTests
{
    private MeterListener _meterListener = null!;
    private readonly List<(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags)> _longMeasurements = new();
    private readonly List<(string InstrumentName, double Value, KeyValuePair<string, object?>[] Tags)> _doubleMeasurements = new();

    [SetUp]
    public void SetUp()
    {
        _longMeasurements.Clear();
        _doubleMeasurements.Clear();

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == DiagnosticsConfig.ServiceName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback<long>(
            (instrument, measurement, tags, _) =>
            {
                _longMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
            });

        _meterListener.SetMeasurementEventCallback<double>(
            (instrument, measurement, tags, _) =>
            {
                _doubleMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
            });

        _meterListener.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _meterListener.Dispose();
    }

    [Test]
    public void RecordReceived_IncrementsReceivedCounterAndInFlight()
    {
        PlatformMeters.RecordReceived("OrderCreated", "OrderService");
        _meterListener.RecordObservableInstruments();

        var received = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.received").ToList();
        var inFlight = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.in_flight").ToList();

        Assert.That(received, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(received.Any(m => m.Value == 1), Is.True);
        Assert.That(inFlight, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(inFlight.Any(m => m.Value == 1), Is.True);
    }

    [Test]
    public void RecordProcessed_IncrementsProcessedAndDecrementsInFlightAndRecordsDuration()
    {
        PlatformMeters.RecordProcessed("OrderCreated", 42.5);
        _meterListener.RecordObservableInstruments();

        var processed = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.processed").ToList();
        var inFlight = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.in_flight").ToList();
        var duration = _doubleMeasurements.Where(m => m.InstrumentName == "eip.messages.processing_duration").ToList();

        Assert.That(processed, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(processed.Any(m => m.Value == 1), Is.True);
        Assert.That(inFlight, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(inFlight.Any(m => m.Value == -1), Is.True);
        Assert.That(duration, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(duration.Any(m => m.Value == 42.5), Is.True);
    }

    [Test]
    public void RecordFailed_IncrementsFailedAndDecrementsInFlight()
    {
        PlatformMeters.RecordFailed("OrderCreated");
        _meterListener.RecordObservableInstruments();

        var failed = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.failed").ToList();
        var inFlight = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.in_flight").ToList();

        Assert.That(failed, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(failed.Any(m => m.Value == 1), Is.True);
        Assert.That(inFlight, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(inFlight.Any(m => m.Value == -1), Is.True);
    }

    [Test]
    public void RecordDeadLettered_IncrementsDeadLetteredCounter()
    {
        PlatformMeters.RecordDeadLettered("OrderCreated");
        _meterListener.RecordObservableInstruments();

        var deadLettered = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.dead_lettered").ToList();

        Assert.That(deadLettered, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(deadLettered.Any(m => m.Value == 1), Is.True);
    }

    [Test]
    public void RecordRetry_IncrementsRetriedCounterWithRetryCountTag()
    {
        PlatformMeters.RecordRetry("OrderCreated", 3);
        _meterListener.RecordObservableInstruments();

        var retried = _longMeasurements.Where(m => m.InstrumentName == "eip.messages.retried").ToList();

        Assert.That(retried, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(retried.Any(m => m.Value == 1), Is.True);
        var retryTag = retried.First(m => m.Value == 1).Tags.FirstOrDefault(t => t.Key == "eip.retry.count");
        Assert.That(retryTag.Value, Is.EqualTo(3));
    }
}

[TestFixture]
public class MessageTracerTests
{
    private ActivityListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown()
    {
        _listener.Dispose();
        Activity.Current = null;
    }

    [Test]
    public void TraceIngestion_StartsActivityWithIngestionStage()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "OrderService", "OrderCreated");

        using var activity = MessageTracer.TraceIngestion(envelope);

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.OperationName, Is.EqualTo(MessageTracer.StageIngestion));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagStage), Is.EqualTo("Ingestion"));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagDeliveryStatus), Is.EqualTo("InFlight"));
    }

    [Test]
    public void TraceRouting_StartsActivityWithRoutingStage()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "OrderService", "OrderCreated");

        using var activity = MessageTracer.TraceRouting(envelope);

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.OperationName, Is.EqualTo(MessageTracer.StageRouting));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagStage), Is.EqualTo("Routing"));
    }

    [Test]
    public void TraceTransformation_StartsActivityWithTransformationStage()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "OrderService", "OrderCreated");

        using var activity = MessageTracer.TraceTransformation(envelope);

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.OperationName, Is.EqualTo(MessageTracer.StageTransformation));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagStage), Is.EqualTo("Transformation"));
    }

    [Test]
    public void TraceDelivery_StartsActivityWithDeliveryStage()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "OrderService", "OrderCreated");

        using var activity = MessageTracer.TraceDelivery(envelope);

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.OperationName, Is.EqualTo(MessageTracer.StageDelivery));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagStage), Is.EqualTo("Delivery"));
    }

    [Test]
    public void CompleteSuccess_SetsOkStatusAndDeliveredTag()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test-success")!;

        MessageTracer.CompleteSuccess(activity, "OrderCreated", 100.0);

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagDeliveryStatus), Is.EqualTo("Delivered"));
    }

    [Test]
    public void CompleteFailed_SetsErrorStatusAndRecordsException()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test-fail")!;
        var ex = new InvalidOperationException("something broke");

        MessageTracer.CompleteFailed(activity, "OrderCreated", ex);

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagDeliveryStatus), Is.EqualTo("Failed"));
        Assert.That(activity.Events.Count(e => e.Name == "exception"), Is.EqualTo(1));
    }
}
