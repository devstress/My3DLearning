// ============================================================================
// Tutorial 23 – Request-Reply (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Request-Reply pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Request envelope has Intent=Command and ReplyTo set correctly
//   🟡 Intermediate — Concurrent requests correlate replies to the correct caller
//   🔴 Advanced     — Timeout duration is within a reasonable range
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.RequestReply;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial23;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Request envelope has intent and ReplyTo ────────────
    //
    // SCENARIO: A request-reply correlator sends a request with a specific
    //           CorrelationId. The published envelope must have ReplyTo set
    //           to the reply topic, Intent = Command, and the correct
    //           CorrelationId.
    //
    // WHAT YOU PROVE: The correlator correctly stamps the outgoing request
    //                 envelope with all required routing properties.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_RequestEnvelope_HasIntentAndReplyTo()
    {
        await using var producer = new MockEndpoint("exam-prod");
        await using var consumer = new MockEndpoint("exam-cons");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 200);
        var correlationId = Guid.NewGuid();
        var request = new RequestReplyMessage<string>(
            "payload", "req-topic", "rep-topic", "source", "type", correlationId);

        await correlator.SendAndReceiveAsync(request);

        var sent = producer.GetReceived<string>(0);
        Assert.That(sent.ReplyTo, Is.EqualTo("rep-topic"));
        Assert.That(sent.Intent, Is.EqualTo(MessageIntent.Command));
        Assert.That(sent.CorrelationId, Is.EqualTo(correlationId));
        producer.AssertReceivedOnTopic("req-topic", 1);
    }

    // ── 🟡 INTERMEDIATE — Concurrent requests correlate correctly ──────
    //
    // SCENARIO: Two requests are sent concurrently with different CorrelationIds.
    //           A reply is sent for corr1 after a short delay. The result must
    //           match the correct CorrelationId and contain the expected payload.
    //
    // WHAT YOU PROVE: The correlator correctly matches replies to their
    //                 originating requests even under concurrent usage.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_ConcurrentRequests_CorrelateCorrectly()
    {
        await using var producer = new MockEndpoint("exam-conc-prod");
        await using var consumer = new MockEndpoint("exam-conc-cons");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 2000);

        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var req1 = new RequestReplyMessage<string>(
            "r1", "req-topic", "rep-topic", "svc", "type", corr1);
        var req2 = new RequestReplyMessage<string>(
            "r2", "req-topic", "rep-topic", "svc", "type", corr2);

        var task1 = correlator.SendAndReceiveAsync(req1);

        // Give the first request time to subscribe
        await Task.Delay(50);

        // Send reply for corr1
        var reply1 = IntegrationEnvelope<string>.Create("ans1", "be", "resp", corr1);
        await consumer.SendAsync(reply1);

        var result1 = await task1;

        Assert.That(result1.TimedOut, Is.False);
        Assert.That(result1.Reply!.Payload, Is.EqualTo("ans1"));
        Assert.That(result1.CorrelationId, Is.EqualTo(corr1));
    }

    // ── 🔴 ADVANCED — Timeout duration is within a reasonable range ─────
    //
    // SCENARIO: A request is sent with a 300ms timeout and no reply arrives.
    //           The result must show TimedOut = true, Reply = null, and a
    //           Duration between 200ms and 2000ms.
    //
    // WHAT YOU PROVE: The correlator respects the configured timeout and
    //                 accurately reports the elapsed duration.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_Timeout_DurationIsReasonable()
    {
        await using var producer = new MockEndpoint("exam-to-prod");
        await using var consumer = new MockEndpoint("exam-to-cons");
        var correlator = CreateCorrelator(producer, consumer, timeoutMs: 300);

        var request = new RequestReplyMessage<string>(
            "data", "req", "rep", "svc", "type");

        var result = await correlator.SendAndReceiveAsync(request);

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Reply, Is.Null);
        Assert.That(result.Duration.TotalMilliseconds, Is.GreaterThan(200));
        Assert.That(result.Duration.TotalMilliseconds, Is.LessThan(2000));
    }

    private static RequestReplyCorrelator<string, string> CreateCorrelator(
        MockEndpoint producer, MockEndpoint consumer, int timeoutMs)
    {
        var options = Options.Create(new RequestReplyOptions
        {
            TimeoutMs = timeoutMs,
            ConsumerGroup = "exam-group",
        });

        return new RequestReplyCorrelator<string, string>(
            producer, consumer, options,
            NullLogger<RequestReplyCorrelator<string, string>>.Instance);
    }
}
