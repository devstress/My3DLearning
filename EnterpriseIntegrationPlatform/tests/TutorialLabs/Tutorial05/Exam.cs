// ============================================================================
// Tutorial 05 – Message Brokers (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply message broker patterns in realistic
//          scenarios — multi-broker fan-out, selective filtering, and
//          DI-wired pipelines. Each challenge is progressively harder.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Fan out one event to NATS + Kafka + Pulsar simultaneously
//   🟡 Intermediate — Priority-based triage with selective consumer predicate
//   🔴 Advanced     — AspireIntegrationTestHost DI pipeline with BrokerOptions
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint / AspireIntegrationTestHost
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

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

        var envelope = IntegrationEnvelope<string>.Create(
            "critical-event", "AlertService", "alert.raised") with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Event,
        };

        // Fan out to all three broker endpoints
        await nats.PublishAsync(envelope, "alerts");
        await kafka.PublishAsync(envelope, "alerts");
        await pulsar.PublishAsync(envelope, "alerts");

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

        await output.SubscribeAsync<string>("orders", "triage-group",
            env => env.Priority == MessagePriority.High
                || env.Priority == MessagePriority.Critical,
            msg =>
            {
                triaged.Add($"{msg.Priority}:{msg.Payload}");
                return Task.CompletedTask;
            });

        // Send messages at all priority levels
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "low-order", "svc", "order") with { Priority = MessagePriority.Low });
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "normal-order", "svc", "order") with { Priority = MessagePriority.Normal });
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "high-order", "svc", "order") with { Priority = MessagePriority.High });
        await output.SendAsync(IntegrationEnvelope<string>.Create(
            "critical-order", "svc", "order") with { Priority = MessagePriority.Critical });

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
        builder.UseProducer(output);
        builder.Configure<BrokerOptions>(opts =>
        {
            opts.BrokerType = BrokerType.NatsJetStream;
            opts.ConnectionString = "nats://localhost:15222";
            opts.TransactionTimeoutSeconds = 60;
        });
        await using var host = builder.Build();

        // Resolve the producer from DI — it's the MockEndpoint
        var producer = host.GetService<IMessageBrokerProducer>();
        var envelope = IntegrationEnvelope<string>.Create(
            "di-message", "DIService", "di.event") with
        {
            Intent = MessageIntent.Event,
            Priority = MessagePriority.High,
        };

        await producer.PublishAsync(envelope, "di-topic");

        output.AssertReceivedCount(1);
        var received = output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("di-message"));
        Assert.That(received.Intent, Is.EqualTo(MessageIntent.Event));

        await output.DisposeAsync();
    }
}
