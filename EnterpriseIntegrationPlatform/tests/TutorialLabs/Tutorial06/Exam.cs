// ============================================================================
// Tutorial 06 – Messaging Channels (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply messaging channel patterns in realistic
//          scenarios — bridging between channels, fan-out verification,
//          and multi-type routing.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Bridge messages between two Point-to-Point channels
//   🟡 Intermediate — Pub-Sub fan-out delivers to all three subscriber groups
//   🔴 Advanced     — Datatype Channel routes multiple message types to correct topics
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
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

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

        await inbound.ReceiveAsync<string>("inbound-q", "bridge-group",
            async msg => await outbound.SendAsync(msg, "outbound-q", CancellationToken.None),
            CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create(
            "bridged-payload", "SourceSystem", "source.event");
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
        await channel.SubscribeAsync<string>("notifications", "email",
            msg => { results.Add("email"); return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "sms",
            msg => { results.Add("sms"); return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "push",
            msg => { results.Add("push"); return Task.CompletedTask; }, CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create(
            "broadcast", "NotificationService", "notification.sent");
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
        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "dt", Separator = "." });
        var channel = new DatatypeChannel(
            endpoint, options, NullLogger<DatatypeChannel>.Instance);

        var orderEnv = IntegrationEnvelope<string>.Create("o1", "svc", "order.created");
        var paymentEnv = IntegrationEnvelope<string>.Create("p1", "svc", "payment.received");
        var inventoryEnv = IntegrationEnvelope<string>.Create("i1", "svc", "inventory.updated");

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
