// ============================================================================
// Tutorial 39 – Message Lifecycle / System Management (Exam)
// ============================================================================
// Coding challenges: full SmartProxy lifecycle, TestMessageGenerator with
// custom payload, and ControlBus publish command verification.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial39;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full SmartProxy Lifecycle ──────────────────────────────

    [Test]
    public void Challenge1_FullSmartProxyLifecycle()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);

        // Track three requests
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
        var duplicateReply = IntegrationEnvelope<string>.Create(
            "dup", "ReplySvc", "cmd.response",
            correlationId: req2.CorrelationId);

        Assert.That(proxy.CorrelateReply(duplicateReply), Is.Null);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));
    }

    // ── Challenge 2: TestMessageGenerator with Custom Payload ───────────────

    [Test]
    public async Task Challenge2_TestMessageGenerator_CustomPayload()
    {
        IntegrationEnvelope<Dictionary<string, object>>? captured = null;
        var producer = Substitute.For<IMessageBrokerProducer>();
        producer.PublishAsync(
                Arg.Any<IntegrationEnvelope<Dictionary<string, object>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
                captured = ci.ArgAt<IntegrationEnvelope<Dictionary<string, object>>>(0));

        var generator = new TestMessageGenerator(
            producer, NullLogger<TestMessageGenerator>.Instance);

        var customPayload = new Dictionary<string, object>
        {
            ["orderId"] = "ORD-42",
            ["amount"] = 99.95,
        };

        var result = await generator.GenerateAsync(
            customPayload, "custom-topic", CancellationToken.None);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("custom-topic"));
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload["orderId"], Is.EqualTo("ORD-42"));
    }

    // ── Challenge 3: ControlBus Publish Command Verification ────────────────

    [Test]
    public async Task Challenge3_ControlBusPublishCommand_Verification()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var consumer = Substitute.For<IMessageBrokerConsumer>();

        var opts = Options.Create(new ControlBusOptions
        {
            ControlTopic = "eip.control",
            ConsumerGroup = "ctrl-group",
            Source = "TestBus",
        });

        var publisher = new ControlBusPublisher(
            producer, consumer, opts, NullLogger<ControlBusPublisher>.Instance);

        var command = new { Action = "restart", Target = "router-1" };
        var result = await publisher.PublishCommandAsync(
            command, "system.restart", CancellationToken.None);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ControlTopic, Is.EqualTo("eip.control"));

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<object>>(),
            "eip.control",
            Arg.Any<CancellationToken>());
    }

    // ── Helper ──────────────────────────────────────────────────────────────

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
