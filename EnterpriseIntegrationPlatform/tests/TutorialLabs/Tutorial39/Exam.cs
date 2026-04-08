// ============================================================================
// Tutorial 39 – Message Lifecycle (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟡 Intermediate  — control bus_ publish multiple commands_ mock endpoint
//   🔴 Advanced      — smart proxy_ and_ control bus_ combined e2 e
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial39;

[TestFixture]
public sealed class Exam
{
    [Test]
    public void Starter_FullSmartProxyLifecycle()
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
    public async Task Intermediate_ControlBus_PublishMultipleCommands_MockEndpoint()
    {
        await using var bus = new MockEndpoint("exam-ctrl-bus");

        // TODO: Create a ControlBusPublisher with appropriate configuration
        dynamic publisher = null!;

        // TODO: var r1 = await publisher.PublishCommandAsync(...)
        dynamic r1 = null!;
        // TODO: var r2 = await publisher.PublishCommandAsync(...)
        dynamic r2 = null!;
        // TODO: var r3 = await publisher.PublishCommandAsync(...)
        dynamic r3 = null!;

        Assert.That(r1.Succeeded, Is.True);
        Assert.That(r2.Succeeded, Is.True);
        Assert.That(r3.Succeeded, Is.True);
        bus.AssertReceivedOnTopic("eip.control", 3);
    }

    [Test]
    public async Task Advanced_SmartProxy_And_ControlBus_CombinedE2E()
    {
        await using var bus = new MockEndpoint("exam-combined");
        // TODO: Create a SmartProxy with appropriate configuration
        dynamic proxy = null!;

        // Track a request through SmartProxy
        var request = CreateEnvelopeWithReplyTo(
            "query-data", "ClientSvc", "data.query", "client-reply-queue");
        Assert.That(proxy.TrackRequest(request), Is.True);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));

        // Publish tracking event to ControlBus
        // TODO: Create a ControlBusPublisher with appropriate configuration
        dynamic publisher = null!;

        // TODO: var trackResult = await publisher.PublishCommandAsync(...)
        dynamic trackResult = null!;

        Assert.That(trackResult.Succeeded, Is.True);
        bus.AssertReceivedOnTopic("eip.control", 1);

        // Simulate reply arriving — correlate it
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic reply = null!;
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
#endif
