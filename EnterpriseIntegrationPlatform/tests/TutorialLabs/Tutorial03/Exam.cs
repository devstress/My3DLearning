// ============================================================================
// Tutorial 03 – Your First Message (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply envelope construction, causation chains, and
//          channel semantics in realistic integration scenarios. Each challenge
//          is progressively harder and builds on concepts from the Lab.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Three-generation causation chain with shared CorrelationId
//   🟡 Intermediate — P2P vs PubSub delivery semantics side-by-side
//   🔴 Advanced     — Priority, intent, expiration, and metadata lifecycle
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint / PointToPointChannel / PublishSubscribeChannel
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

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

        var orderCreated = IntegrationEnvelope<string>.Create(
            "ORD-500", "OrderService", "order.created");

        var orderValidated = IntegrationEnvelope<string>.Create(
            "ORD-500-valid", "ValidationService", "order.validated",
            correlationId: orderCreated.CorrelationId,
            causationId: orderCreated.MessageId);

        var orderFulfilled = IntegrationEnvelope<string>.Create(
            "ORD-500-shipped", "FulfillmentService", "order.fulfilled",
            correlationId: orderCreated.CorrelationId,
            causationId: orderValidated.MessageId);

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

        var p2pChannel = new PointToPointChannel(
            p2pOutput, p2pOutput, NullLogger<PointToPointChannel>.Instance);

        var pubSubChannelA = new PublishSubscribeChannel(
            pubSubA, pubSubA, NullLogger<PublishSubscribeChannel>.Instance);
        var pubSubChannelB = new PublishSubscribeChannel(
            pubSubB, pubSubB, NullLogger<PublishSubscribeChannel>.Instance);

        // Send 3 messages through P2P — all go to the single output
        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"cmd-{i}", "svc", "command");
            await p2pChannel.SendAsync(env, "commands", CancellationToken.None);
        }

        // Send 2 messages through Pub/Sub — each subscriber gets both
        for (var i = 0; i < 2; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"event-{i}", "svc", "event");
            await pubSubChannelA.PublishAsync(env, "events", CancellationToken.None);
            await pubSubChannelB.PublishAsync(env, "events", CancellationToken.None);
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

        // Critical command that expires in 1 hour
        var urgentCommand = IntegrationEnvelope<string>.Create(
            "shutdown-node-5", "OpsService", "infra.command") with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Command,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.TraceId] = "trace-urgent-001",
            },
        };

        // Low-priority document (no expiration)
        var backgroundDoc = IntegrationEnvelope<string>.Create(
            "monthly-report-data", "ReportService", "report.document") with
        {
            Priority = MessagePriority.Low,
            Intent = MessageIntent.Document,
        };

        // Already-expired event
        var expiredEvent = IntegrationEnvelope<string>.Create(
            "stale-price-update", "PricingService", "price.event") with
        {
            Priority = MessagePriority.Normal,
            Intent = MessageIntent.Event,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
        };

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
