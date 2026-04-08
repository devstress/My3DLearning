// ============================================================================
// Tutorial 06 – Messaging Channels (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Bridge messages between two Point-to-Point channels
//   🟡 Intermediate — Pub-Sub fan-out delivers to all three subscriber groups
//   🔴 Advanced     — Datatype Channel routes multiple message types to correct topics
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial06;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Point-to-Point Bridge Relay ──────────────────────────
    //
    // SCENARIO: A legacy order system publishes to an inbound queue. A bridge
    // component must relay each message to an outbound queue for the modern
    // fulfilment system. Both channels use Point-to-Point semantics.
    //
    // WHAT YOU PROVE: You can wire ReceiveAsync on one channel to SendAsync
    // on another, creating a messaging bridge between two endpoints.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_Bridge_PointToPointRelay()
    {
        await using var source = new MockEndpoint("source");
        await using var target = new MockEndpoint("target");

        var inbound = new PointToPointChannel(
            source, source, NullLogger<PointToPointChannel>.Instance);
        var outbound = new PointToPointChannel(
            target, target, NullLogger<PointToPointChannel>.Instance);

        // TODO: Wire ReceiveAsync on inbound channel — when a message arrives on "inbound-q"
        //   in group "bridge-group", forward it to outbound channel on topic "outbound-q"
        await inbound.ReceiveAsync<string>("inbound-q", "bridge-group",
            async msg =>
            {
                // TODO: forward the message to outbound channel
                await Task.CompletedTask; // ← replace with outbound.SendAsync(...)
            },
            CancellationToken.None);

        // TODO: Create an IntegrationEnvelope<string> with payload "bridged-payload", source "SourceSystem", type "source.event"
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await source.SendAsync(envelope);

        target.AssertReceivedCount(1);
        Assert.That(target.GetReceived<string>().Payload, Is.EqualTo("bridged-payload"));
        target.AssertReceivedOnTopic("outbound-q", 1);
    }

    // ── 🟡 INTERMEDIATE — Pub-Sub Fan-Out Verification ─────────────────────
    //
    // SCENARIO: A notification service broadcasts a single event to three
    // subscriber groups — email, SMS, and push. All three must receive the
    // identical broadcast message.
    //
    // WHAT YOU PROVE: You can set up multiple Pub-Sub subscribers and verify
    // that fan-out delivers the message to every registered group.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_PubSubFanOut_ThreeSubscribersReceive()
    {
        await using var endpoint = new MockEndpoint("fanout");
        var channel = new PublishSubscribeChannel(
            endpoint, endpoint, NullLogger<PublishSubscribeChannel>.Instance);

        var results = new List<string>();
        // TODO: Subscribe three groups ("email", "sms", "push") to "notifications" topic.
        //   Each handler should add its group name to the results list and return Task.CompletedTask.
        await channel.SubscribeAsync<string>("notifications", "email",
            msg => { /* TODO: results.Add("email"); */ return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "sms",
            msg => { /* TODO: results.Add("sms"); */ return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "push",
            msg => { /* TODO: results.Add("push"); */ return Task.CompletedTask; }, CancellationToken.None);

        // TODO: Create an IntegrationEnvelope<string> with payload "broadcast", source "NotificationService", type "notification.sent"
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        await endpoint.SendAsync(envelope);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results, Does.Contain("email"));
        Assert.That(results, Does.Contain("sms"));
        Assert.That(results, Does.Contain("push"));
    }

    // ── 🔴 ADVANCED — Multi-Type Datatype Channel Routing ──────────────────
    //
    // SCENARIO: An integration hub receives order, payment, and inventory
    // events. Each message type must be automatically routed to its own
    // dedicated topic using the Datatype Channel pattern.
    //
    // WHAT YOU PROVE: You can configure DatatypeChannel to route multiple
    // message types to type-specific topics and verify per-topic delivery.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_DatatypeChannel_MultipleTypesRoutedCorrectly()
    {
        await using var endpoint = new MockEndpoint("datatype");
        // TODO: Create DatatypeChannelOptions with TopicPrefix = "dt" and Separator = "."
        var options = Options.Create(new DatatypeChannelOptions()); // ← replace with { TopicPrefix = "dt", Separator = "." }
        // TODO: Create a DatatypeChannel using endpoint, options, and NullLogger
        DatatypeChannel channel = null!; // ← replace with new DatatypeChannel(...)

        // TODO: Create three envelopes for order.created ("o1"), payment.received ("p1"), inventory.updated ("i1")
        IntegrationEnvelope<string> orderEnv = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        IntegrationEnvelope<string> paymentEnv = null!; // ← replace with IntegrationEnvelope<string>.Create(...)
        IntegrationEnvelope<string> inventoryEnv = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        await channel.PublishAsync(orderEnv, CancellationToken.None);
        await channel.PublishAsync(paymentEnv, CancellationToken.None);
        await channel.PublishAsync(inventoryEnv, CancellationToken.None);

        endpoint.AssertReceivedCount(3);
        endpoint.AssertReceivedOnTopic("dt.order.created", 1);
        endpoint.AssertReceivedOnTopic("dt.payment.received", 1);
        endpoint.AssertReceivedOnTopic("dt.inventory.updated", 1);
        Assert.That(endpoint.GetReceivedTopics(), Has.Count.EqualTo(3));
    }
}
#endif
