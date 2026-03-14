using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class TraceEnricherTests
{
    [Fact]
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

        activity.GetTagItem(PlatformActivitySource.TagMessageId).Should().Be(envelope.MessageId.ToString());
        activity.GetTagItem(PlatformActivitySource.TagCorrelationId).Should().Be(envelope.CorrelationId.ToString());
        activity.GetTagItem(PlatformActivitySource.TagMessageType).Should().Be("TestMessage");
        activity.GetTagItem(PlatformActivitySource.TagSource).Should().Be("TestService");
        activity.GetTagItem(PlatformActivitySource.TagPriority).Should().Be("Normal");
    }

    [Fact]
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

        activity.GetTagItem(PlatformActivitySource.TagDeliveryStatus).Should().Be("Delivered");
    }

    [Fact]
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

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("something broke");
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }
}
