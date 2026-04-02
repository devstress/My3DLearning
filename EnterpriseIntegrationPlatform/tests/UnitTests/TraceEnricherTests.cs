using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TraceEnricherTests
{
    [Test]
    public void Enrich_SetsAllStandardTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test")!;
        var envelope = IntegrationEnvelope<string>.Create(
            payload: "data",
            source: "TestService",
            messageType: "TestMessage");

        TraceEnricher.Enrich(activity, envelope);

        Assert.That(activity.GetTagItem(PlatformActivitySource.TagMessageId), Is.EqualTo(envelope.MessageId.ToString()));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagCorrelationId), Is.EqualTo(envelope.CorrelationId.ToString()));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagMessageType), Is.EqualTo("TestMessage"));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagSource), Is.EqualTo("TestService"));
        Assert.That(activity.GetTagItem(PlatformActivitySource.TagPriority), Is.EqualTo("Normal"));
    }

    [Test]
    public void SetDeliveryStatus_SetsTag()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test")!;

        TraceEnricher.SetDeliveryStatus(activity, DeliveryStatus.Delivered);

        Assert.That(activity.GetTagItem(PlatformActivitySource.TagDeliveryStatus), Is.EqualTo("Delivered"));
    }

    [Test]
    public void RecordException_SetsErrorStatusAndAddsEvent()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("test")!;
        var ex = new InvalidOperationException("something broke");

        TraceEnricher.RecordException(activity, ex);

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(activity.StatusDescription, Is.EqualTo("something broke"));
        Assert.That(activity.Events.Count(e => e.Name == "exception"), Is.EqualTo(1));
    }
}
