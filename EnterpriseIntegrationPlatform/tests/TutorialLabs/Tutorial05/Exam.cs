// ============================================================================
// Tutorial 05 – Message Brokers (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Fan out one event to NATS + Kafka + Pulsar simultaneously
//   🟡 Intermediate — Priority-based triage with selective consumer predicate
//   🔴 Advanced     — AspireIntegrationTestHost DI pipeline with BrokerOptions
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Multi-Broker Fan-Out ──────────────────────────────
    //
    // SCENARIO: An alert service raises a critical event that must reach
    // three broker endpoints simultaneously: NATS for real-time processing,
    // Kafka for audit logging, and Pulsar for partner delivery. All three
    // must receive the identical message with the same identity.
    //
    // WHAT YOU PROVE: You can fan out a single event to multiple broker
    // endpoints and verify consistent delivery across all of them.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_MultiBrokerFanOut_AllEndpointsReceive()
    {
        // In a multi-broker topology, the same event must be published to
        // all broker endpoints simultaneously — NATS for real-time, Kafka
        // for audit, Pulsar for partner delivery.
        var nats = new MockEndpoint("nats");
        var kafka = new MockEndpoint("kafka");
        var pulsar = new MockEndpoint("pulsar");

        // TODO: Create an IntegrationEnvelope<string> with payload "critical-event", source "AlertService", type "alert.raised", Priority = Critical, Intent = Event
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        // TODO: Fan out — publish the envelope to all three broker endpoints on topic "alerts"
        await Task.CompletedTask; // ← replace with nats.PublishAsync(...)
        // await kafka.PublishAsync(...)
        // await pulsar.PublishAsync(...)

        nats.AssertReceivedCount(1);
        kafka.AssertReceivedCount(1);
        pulsar.AssertReceivedCount(1);

        // Same payload, same identity across all brokers
        Assert.That(nats.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(kafka.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(pulsar.GetReceived<string>().Priority, Is.EqualTo(MessagePriority.Critical));

        await nats.DisposeAsync();
        await kafka.DisposeAsync();
        await pulsar.DisposeAsync();
    }

    // ── 🟡 INTERMEDIATE — Selective Consumer Priority Gate ─────────────
    //
    // SCENARIO: A triage system processes incoming orders but only handles
    // High and Critical priority messages. Low and Normal priority orders
    // are filtered out by a selective consumer predicate. Four orders at
    // different priority levels are submitted; only two should pass.
    //
    // WHAT YOU PROVE: You can configure a selective consumer with a
    // priority-based predicate gate that filters messages before delivery.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_SelectiveConsumer_PriorityGate()
    {
        // ISelectiveConsumer applies a predicate gate before invoking the
        // handler. Only messages matching the predicate are delivered.
        // This simulates a priority-based triage system.
        var output = new MockEndpoint("triage");
        var triaged = new List<string>();

        // TODO: Subscribe to "orders" topic with group "triage-group".
        //   Predicate: only accept High or Critical priority.
        //   Handler: add "{msg.Priority}:{msg.Payload}" to triaged list.
        await output.SubscribeAsync<string>("orders", "triage-group",
            env => false, // ← replace predicate: env.Priority == MessagePriority.High || ...
            msg =>
            {
                // TODO: Add the triaged message string to the triaged list
                return Task.CompletedTask;
            });

        // TODO: Send four messages at different priority levels (Low, Normal, High, Critical)
        //   Each with source "svc", type "order", and an appropriate payload string
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "low-order", "svc", "order") with { Priority = MessagePriority.Low });
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "normal-order", "svc", "order") with { Priority = MessagePriority.Normal });
        // TODO: Send High priority order
        // await output.SendAsync(...)
        // TODO: Send Critical priority order
        // await output.SendAsync(...)

        // Only High and Critical pass the gate
        Assert.That(triaged, Has.Count.EqualTo(2));
        Assert.That(triaged[0], Does.Contain("High"));
        Assert.That(triaged[1], Does.Contain("Critical"));

        await output.DisposeAsync();
    }

    // ── 🔴 ADVANCED — AspireIntegrationTestHost DI Pipeline ────────────
    //
    // SCENARIO: A production-like DI container is configured through
    // AspireIntegrationTestHost with BrokerOptions (NatsJetStream, custom
    // connection string, 60s timeout) and a MockEndpoint as the producer.
    // A high-priority event with intent is published and verified through
    // the full DI-resolved pipeline.
    //
    // WHAT YOU PROVE: You can wire a complete broker pipeline through DI
    // with custom BrokerOptions and verify end-to-end message delivery.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_DIHost_BrokerOptionsConfigured()
    {
        // AspireIntegrationTestHost wires up a DI container with
        // IMessageBrokerProducer pointed at a MockEndpoint, plus
        // BrokerOptions configured for the test scenario.
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var output = builder.AddMockEndpoint("output");
        // TODO: Wire the MockEndpoint as the producer
        // builder.UseProducer(...)

        // TODO: Configure BrokerOptions — set BrokerType to NatsJetStream,
        //   ConnectionString to "nats://localhost:15222", TransactionTimeoutSeconds to 60
        // builder.Configure<BrokerOptions>(opts => { ... });

        await using var host = builder.Build();

        // Resolve the producer from DI — it's the MockEndpoint
        var producer = host.GetService<IMessageBrokerProducer>();
        // TODO: Create an IntegrationEnvelope<string> with payload "di-message", source "DIService", type "di.event", Intent = Event, Priority = High
        IntegrationEnvelope<string> envelope = null!; // ← replace with IntegrationEnvelope<string>.Create(...)

        await producer.PublishAsync(envelope, "di-topic");

        output.AssertReceivedCount(1);
        var received = output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("di-message"));
        Assert.That(received.Intent, Is.EqualTo(MessageIntent.Event));

        await output.DisposeAsync();
    }
}
#endif
