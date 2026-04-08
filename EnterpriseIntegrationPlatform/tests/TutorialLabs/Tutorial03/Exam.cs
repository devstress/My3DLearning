// ============================================================================
// Tutorial 03 – Your First Message (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Three-generation causation chain with shared CorrelationId
//   🟡 Intermediate — P2P vs PubSub delivery semantics side-by-side
//   🔴 Advanced     — Priority, intent, expiration, and metadata lifecycle
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CS0219 // Variable is assigned but its value is never used (expected in fill-in-blank exam)

namespace TutorialLabs.Tutorial03;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Three-Generation Causation Chain ──────────────────
    //
    // SCENARIO: An e-commerce platform processes an order through three
    // services: OrderService creates it, ValidationService validates it,
    // FulfillmentService ships it. Each message must carry the correct
    // causation lineage so distributed tracing can reconstruct the flow.
    //
    // WHAT YOU PROVE: You can build a multi-generation causation chain
    // where all messages share a CorrelationId and each child references
    // its parent via CausationId.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_CausationChain_ThreeGenerationLineage()
    {
        // Build a three-generation message lineage:
        //   Order.Created → Order.Validated → Order.Fulfilled
        // Each child's CausationId = parent's MessageId.
        // All share the same CorrelationId for distributed tracing.
        var output = new MockEndpoint("lineage");

        // TODO: Create an IntegrationEnvelope<string> with payload "ORD-500", source "OrderService", type "order.created"
        IntegrationEnvelope<string> orderCreated = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // TODO: Create an IntegrationEnvelope<string> with payload "ORD-500-valid", source "ValidationService",
        //       type "order.validated", correlationId=orderCreated.CorrelationId, causationId=orderCreated.MessageId
        IntegrationEnvelope<string> orderValidated = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // TODO: Create an IntegrationEnvelope<string> with payload "ORD-500-shipped", source "FulfillmentService",
        //       type "order.fulfilled", correlationId=orderCreated.CorrelationId, causationId=orderValidated.MessageId
        IntegrationEnvelope<string> orderFulfilled = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // Publish all three to trace the lineage
        await output.PublishAsync(orderCreated, "order-events");
        await output.PublishAsync(orderValidated, "order-events");
        await output.PublishAsync(orderFulfilled, "order-events");

        output.AssertReceivedCount(3);

        // All share the same correlation (business transaction)
        var messages = output.GetAllReceived<string>("order-events");
        Assert.That(messages.Select(m => m.CorrelationId).Distinct().Count(), Is.EqualTo(1));

        // Causation chain: Created→Validated→Fulfilled
        Assert.That(orderCreated.CausationId, Is.Null); // root has no parent
        Assert.That(orderValidated.CausationId, Is.EqualTo(orderCreated.MessageId));
        Assert.That(orderFulfilled.CausationId, Is.EqualTo(orderValidated.MessageId));

        // Each has a unique MessageId
        var ids = new[] { orderCreated.MessageId, orderValidated.MessageId, orderFulfilled.MessageId };
        Assert.That(ids.Distinct().Count(), Is.EqualTo(3));

        await output.DisposeAsync();
    }

    // ── 🟡 INTERMEDIATE — P2P vs Pub/Sub Channel Semantics ────────────
    //
    // SCENARIO: A platform uses P2P channels for commands (one consumer per
    // message) and PubSub channels for events (all subscribers receive).
    // You must demonstrate both patterns side-by-side and verify the
    // fundamental delivery difference.
    //
    // WHAT YOU PROVE: You understand the semantic difference between P2P
    // (queue) and PubSub (fan-out) channels and can wire both correctly.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_PointToPointVsPubSub_ChannelSemantics()
    {
        // Demonstrate the fundamental difference:
        // - Point-to-Point: one consumer receives each message
        // - Pub/Sub: ALL subscribers receive each message
        var p2pOutput = new MockEndpoint("p2p");
        var pubSubA = new MockEndpoint("sub-a");
        var pubSubB = new MockEndpoint("sub-b");

        // TODO: Create a PointToPointChannel using p2pOutput for both sender and receiver, with NullLogger.
        PointToPointChannel p2pChannel = null!; // ← replace with new PointToPointChannel(...)

        // TODO: Create two PublishSubscribeChannels — one using pubSubA, one using pubSubB, with NullLogger.
        PublishSubscribeChannel pubSubChannelA = null!; // ← replace with new PublishSubscribeChannel(...)
        PublishSubscribeChannel pubSubChannelB = null!; // ← replace with new PublishSubscribeChannel(...)

        // Send 3 messages through P2P — all go to the single output
        for (var i = 0; i < 3; i++)
        {
            // TODO: Create an IntegrationEnvelope<string> with payload $"cmd-{i}", source "svc", type "command"
            //       and send it via p2pChannel to "commands" topic.
        }

        // Send 2 messages through Pub/Sub — each subscriber gets both
        for (var i = 0; i < 2; i++)
        {
            // TODO: Create an IntegrationEnvelope<string> with payload $"event-{i}", source "svc", type "event"
            //       and publish it via both pubSubChannelA and pubSubChannelB to "events" topic.
        }

        // P2P: 3 messages, single consumer
        p2pOutput.AssertReceivedCount(3);

        // Pub/Sub: each subscriber gets 2 messages independently
        pubSubA.AssertReceivedCount(2);
        pubSubB.AssertReceivedCount(2);
        Assert.That(pubSubA.GetReceived<string>(0).Payload, Is.EqualTo("event-0"));
        Assert.That(pubSubB.GetReceived<string>(1).Payload, Is.EqualTo("event-1"));

        await p2pOutput.DisposeAsync();
        await pubSubA.DisposeAsync();
        await pubSubB.DisposeAsync();
    }

    // ── 🔴 ADVANCED — Priority, Intent, and Expiration Lifecycle ───────
    //
    // SCENARIO: An operations platform processes messages at different
    // priority levels: a critical infrastructure command (expires in 1hr),
    // a low-priority background report (no expiration), and an already-
    // expired price update event. All must be delivered with correct
    // priority, intent, expiration status, and metadata preserved.
    //
    // WHAT YOU PROVE: You can construct envelopes with full lifecycle
    // properties and verify they survive delivery without data loss.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_PriorityExpiration_MessageLifecycle()
    {
        // Build messages with different priorities and intents,
        // verify expiration logic, and confirm metadata survives delivery.
        var output = new MockEndpoint("lifecycle");

        // TODO: Create a Critical command envelope with payload "shutdown-node-5", source "OpsService",
        //       type "infra.command", Priority=Critical, Intent=Command, ExpiresAt=1hr from now,
        //       Metadata with TraceId="trace-urgent-001".
        IntegrationEnvelope<string> urgentCommand = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { ... }

        // TODO: Create a Low-priority document envelope with payload "monthly-report-data",
        //       source "ReportService", type "report.document", Priority=Low, Intent=Document.
        IntegrationEnvelope<string> backgroundDoc = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { ... }

        // TODO: Create an already-expired event envelope with payload "stale-price-update",
        //       source "PricingService", type "price.event", Priority=Normal, Intent=Event,
        //       ExpiresAt=1hr in the past.
        IntegrationEnvelope<string> expiredEvent = null!; // ← replace with IntegrationEnvelope<string>.Create(...) with { ... }

        await output.PublishAsync(urgentCommand, "commands");
        await output.PublishAsync(backgroundDoc, "documents");
        await output.PublishAsync(expiredEvent, "events");

        output.AssertReceivedCount(3);

        // Verify priority ordering is preserved
        var cmd = output.GetReceived<string>(0);
        var doc = output.GetReceived<string>(1);
        var evt = output.GetReceived<string>(2);

        Assert.That(cmd.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(doc.Priority, Is.EqualTo(MessagePriority.Low));
        Assert.That(evt.Priority, Is.EqualTo(MessagePriority.Normal));

        // Verify intents
        Assert.That(cmd.Intent, Is.EqualTo(MessageIntent.Command));
        Assert.That(doc.Intent, Is.EqualTo(MessageIntent.Document));
        Assert.That(evt.Intent, Is.EqualTo(MessageIntent.Event));

        // Expiration checks
        Assert.That(cmd.IsExpired, Is.False);
        Assert.That(doc.IsExpired, Is.False);  // no expiration = never expires
        Assert.That(evt.IsExpired, Is.True);   // already past expiration

        // Metadata survives delivery
        Assert.That(cmd.Metadata[MessageHeaders.TraceId], Is.EqualTo("trace-urgent-001"));

        await output.DisposeAsync();
    }
}
