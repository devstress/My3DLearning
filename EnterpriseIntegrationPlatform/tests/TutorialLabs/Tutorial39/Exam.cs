// ============================================================================
// Tutorial 39 – Message Lifecycle / System Management (Exam)
// ============================================================================
// E2E challenges: full SmartProxy lifecycle, ControlBus publish with
// MockEndpoint verification, and SmartProxy + ControlBus combined E2E.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial39;

[TestFixture]
public sealed class Exam
{
    [Test]
    public void Challenge1_FullSmartProxyLifecycle()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        var req1 = CreateEnvelopeWithReplyTo("r1", "Svc", "cmd.a", "reply-1");
        var req2 = CreateEnvelopeWithReplyTo("r2", "Svc", "cmd.b", "reply-2");
        var req3 = CreateEnvelopeWithReplyTo("r3", "Svc", "cmd.c", "reply-3");

        Assert.That(proxy.TrackRequest(req1), Is.True);
        Assert.That(proxy.TrackRequest(req2), Is.True);
        Assert.That(proxy.TrackRequest(req3), Is.True);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(3));

        // Correlate reply for req2
        var reply2 = IntegrationEnvelope<string>.Create(
            "resp2", "ReplySvc", "cmd.response",
            correlationId: req2.CorrelationId);
        var corr2 = proxy.CorrelateReply(reply2);
        Assert.That(corr2, Is.Not.Null);
        Assert.That(corr2!.OriginalReplyTo, Is.EqualTo("reply-2"));
        Assert.That(proxy.OutstandingCount, Is.EqualTo(2));

        // Correlate reply for req1
        var reply1 = IntegrationEnvelope<string>.Create(
            "resp1", "ReplySvc", "cmd.response",
            correlationId: req1.CorrelationId);
        var corr1 = proxy.CorrelateReply(reply1);
        Assert.That(corr1, Is.Not.Null);
        Assert.That(corr1!.OriginalReplyTo, Is.EqualTo("reply-1"));
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));

        // Duplicate reply returns null
        var dup = IntegrationEnvelope<string>.Create(
            "dup", "ReplySvc", "cmd.response",
            correlationId: req2.CorrelationId);
        Assert.That(proxy.CorrelateReply(dup), Is.Null);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Challenge2_ControlBus_PublishMultipleCommands_MockEndpoint()
    {
        await using var bus = new MockEndpoint("exam-ctrl-bus");

        var publisher = new ControlBusPublisher(
            bus, bus,
            Options.Create(new ControlBusOptions
            {
                ControlTopic = "eip.control",
                ConsumerGroup = "ctrl-group",
                Source = "TestBus",
            }),
            NullLogger<ControlBusPublisher>.Instance);

        var r1 = await publisher.PublishCommandAsync("restart", "system.restart");
        var r2 = await publisher.PublishCommandAsync("scale-up", "system.scale");
        var r3 = await publisher.PublishCommandAsync("flush", "system.flush");

        Assert.That(r1.Succeeded, Is.True);
        Assert.That(r2.Succeeded, Is.True);
        Assert.That(r3.Succeeded, Is.True);
        bus.AssertReceivedOnTopic("eip.control", 3);
    }

    [Test]
    public async Task Challenge3_SmartProxy_And_ControlBus_CombinedE2E()
    {
        await using var bus = new MockEndpoint("exam-combined");
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        // Track a request through SmartProxy
        var request = CreateEnvelopeWithReplyTo(
            "query-data", "ClientSvc", "data.query", "client-reply-queue");
        Assert.That(proxy.TrackRequest(request), Is.True);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));

        // Publish tracking event to ControlBus
        var publisher = new ControlBusPublisher(
            bus, bus,
            Options.Create(new ControlBusOptions
            {
                ControlTopic = "eip.control",
                Source = "SmartProxy",
            }),
            NullLogger<ControlBusPublisher>.Instance);

        var trackResult = await publisher.PublishCommandAsync(
            new { RequestId = request.MessageId, ReplyTo = request.ReplyTo },
            "proxy.request.tracked");

        Assert.That(trackResult.Succeeded, Is.True);
        bus.AssertReceivedOnTopic("eip.control", 1);

        // Simulate reply arriving — correlate it
        var reply = IntegrationEnvelope<string>.Create(
            "query-result", "DataSvc", "data.response",
            correlationId: request.CorrelationId);
        var correlation = proxy.CorrelateReply(reply);

        Assert.That(correlation, Is.Not.Null);
        Assert.That(correlation!.OriginalReplyTo, Is.EqualTo("client-reply-queue"));
        Assert.That(proxy.OutstandingCount, Is.EqualTo(0));
    }

    private static IntegrationEnvelope<string> CreateEnvelopeWithReplyTo(
        string payload, string source, string messageType, string replyTo) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Payload = payload,
            ReplyTo = replyTo,
        };
}
