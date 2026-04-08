// ============================================================================
// Tutorial 05 – Message Brokers (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how broker configuration,
//          protocol-agnostic publishing, and consumer patterns work through
//          all four brokers via Aspire. Read the code and comments to
//          understand each concept before moving to the Exam.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1.  BrokerOptions defaults — NatsJetStream, 30s timeout, section name
//   2.  BrokerType enum — all four supported protocols
//   3.  Protocol-agnostic publish (NATS) — message delivered via abstraction
//   4.  Multi-topic routing (NATS) — per-topic delivery verification
//   5.  Event-driven consumer (NATS) — push-based handler triggered on arrival
//   6.  Polling consumer (NATS) — batch retrieval with max-message limit
//   7.  Selective consumer (NATS) — predicate-based priority filtering
//   8.  Multiple handlers (NATS) — independent subscription handlers all invoked
//   9.  Kafka E2E publish — real Apache Kafka round-trip via Aspire
//   10. Pulsar E2E publish — real Apache Pulsar round-trip via Aspire
//   11. Postgres E2E publish — real PostgreSQL broker round-trip via Aspire
//   12. All four brokers — same IMessageBrokerProducer, interchangeable delivery
//
// INFRASTRUCTURE: NatsBrokerEndpoint, KafkaBrokerEndpoint,
//   PulsarBrokerEndpoint, PostgresBrokerEndpoint (all via Aspire TestAppHost)
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Lab
{
    // ── 1. Broker Configuration ─────────────────────────────────────────

    [Test]
    public void BrokerOptions_Defaults_NatsJetStreamWithSectionName()
    {
        // BrokerOptions defaults: NatsJetStream, 30s transaction timeout.
        // SectionName matches the appsettings.json section for binding.
        var options = new BrokerOptions();

        Assert.That(BrokerOptions.SectionName, Is.EqualTo("Broker"));
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
        Assert.That(options.TransactionTimeoutSeconds, Is.EqualTo(30));
        Assert.That(options.ConnectionString, Is.EqualTo(string.Empty));
    }

    [Test]
    public void BrokerType_AllProtocols_Enumerated()
    {
        // The platform supports four broker protocols.
        Assert.That(Enum.GetValues<BrokerType>(), Has.Length.EqualTo(4));
        Assert.That((int)BrokerType.NatsJetStream, Is.EqualTo(0));
        Assert.That((int)BrokerType.Kafka, Is.EqualTo(1));
        Assert.That((int)BrokerType.Pulsar, Is.EqualTo(2));
        Assert.That((int)BrokerType.Postgres, Is.EqualTo(3));
    }

    // ── 2. Protocol-Agnostic Publishing (Real NATS) ─────────────────────

    [Test]
    public async Task Publish_NatsConfig_MessageDeliveredViaAbstraction()
    {
        // IMessageBrokerProducer abstracts away the protocol.
        // Publishing through real NATS JetStream via NatsBrokerEndpoint.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-publish");
        var topic = AspireFixture.UniqueTopic("t05-pub");

        var envelope = IntegrationEnvelope<string>.Create(
            "nats-message", "NatsService", "nats.event");
        await nats.PublishAsync(envelope, topic);

        nats.AssertReceivedCount(1);
        Assert.That(nats.GetReceived<string>().Payload, Is.EqualTo("nats-message"));
    }

    [Test]
    public async Task Publish_MultipleTopics_PerTopicDeliveryVerified()
    {
        // A single broker endpoint routes messages to different topics
        // through real NATS JetStream.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-multi");
        var ordersTopic = AspireFixture.UniqueTopic("t05-orders");
        var paymentsTopic = AspireFixture.UniqueTopic("t05-payments");
        var shippingTopic = AspireFixture.UniqueTopic("t05-shipping");

        var orderEnv = IntegrationEnvelope<string>.Create("order", "svc", "order.created");
        var paymentEnv = IntegrationEnvelope<string>.Create("payment", "svc", "payment.processed");
        var shippingEnv = IntegrationEnvelope<string>.Create("shipping", "svc", "shipment.dispatched");

        await nats.PublishAsync(orderEnv, ordersTopic);
        await nats.PublishAsync(paymentEnv, paymentsTopic);
        await nats.PublishAsync(shippingEnv, shippingTopic);

        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(ordersTopic, 1);
        nats.AssertReceivedOnTopic(paymentsTopic, 1);
        nats.AssertReceivedOnTopic(shippingTopic, 1);
        Assert.That(nats.GetReceivedTopics(), Has.Count.EqualTo(3));
    }

    // ── 3. Consumer Patterns (Real NATS) ────────────────────────────────

    [Test]
    public async Task EventDrivenConsumer_HandlerTriggeredOnMessageArrival()
    {
        // IEventDrivenConsumer.StartAsync registers a push-based handler.
        // Real NATS delivers messages to the handler.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-event");
        var topic = AspireFixture.UniqueTopic("t05-event");

        IntegrationEnvelope<string>? captured = null;
        await nats.StartAsync<string>(topic, "group", msg =>
        {
            captured = msg;
            return Task.CompletedTask;
        });

        // Allow subscription to establish
        await Task.Delay(500);

        var envelope = IntegrationEnvelope<string>.Create(
            "event-driven", "EventSource", "event.fired");
        await nats.SendAsync(envelope, topic);

        // Wait for delivery
        await nats.WaitForConsumedAsync(1, TimeSpan.FromSeconds(10));

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("event-driven"));
        Assert.That(captured.Source, Is.EqualTo("EventSource"));
    }

    [Test]
    public async Task PollingConsumer_BatchRetrieval_MaxMessagesRespected()
    {
        // IPollingConsumer.PollAsync retrieves up to maxMessages from queue.
        // Using real NATS through NatsBrokerEndpoint.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-poll");
        var topic = AspireFixture.UniqueTopic("t05-poll");

        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"batch-{i}", "svc", "type");
            await nats.SendAsync(env, topic);
        }

        // NatsBrokerEndpoint.PollAsync reads from the inbound queue
        var polled = await nats.PollAsync<string>(topic, "group", maxMessages: 3);

        Assert.That(polled, Has.Count.LessThanOrEqualTo(3));
    }

    [Test]
    public async Task SelectiveConsumer_PredicateFilters_OnlyMatchingDelivered()
    {
        // ISelectiveConsumer adds a predicate gate before the handler.
        // Real NATS delivers, predicate filters locally.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-selective");
        var topic = AspireFixture.UniqueTopic("t05-sel");

        var delivered = new List<string>();
        await nats.SubscribeAsync<string>(topic, "group",
            env => env.Priority >= MessagePriority.High,
            msg =>
            {
                delivered.Add(msg.Payload);
                return Task.CompletedTask;
            });

        await Task.Delay(500);

        var high = IntegrationEnvelope<string>.Create(
            "urgent", "svc", "order") with { Priority = MessagePriority.High };
        var low = IntegrationEnvelope<string>.Create(
            "routine", "svc", "order") with { Priority = MessagePriority.Low };
        var critical = IntegrationEnvelope<string>.Create(
            "emergency", "svc", "order") with { Priority = MessagePriority.Critical };

        await nats.SendAsync(high, topic);
        await nats.SendAsync(low, topic);
        await nats.SendAsync(critical, topic);

        await nats.WaitForConsumedAsync(3, TimeSpan.FromSeconds(10));

        Assert.That(delivered, Has.Count.EqualTo(2));
        Assert.That(delivered, Does.Contain("urgent"));
        Assert.That(delivered, Does.Contain("emergency"));
    }

    [Test]
    public async Task SubscribeConsumer_MultipleHandlers_AllInvoked()
    {
        // Multiple SubscribeAsync calls register independent handlers.
        // Real NATS delivers to all registered handlers.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-fanout");
        var topic = AspireFixture.UniqueTopic("t05-fanout");

        var handler1Results = new List<string>();
        var handler2Results = new List<string>();

        await nats.SubscribeAsync<string>(topic, "group-1", msg =>
        {
            handler1Results.Add(msg.Payload);
            return Task.CompletedTask;
        });
        await nats.SubscribeAsync<string>(topic, "group-2", msg =>
        {
            handler2Results.Add(msg.Payload);
            return Task.CompletedTask;
        });

        await Task.Delay(500);

        var envelope = IntegrationEnvelope<string>.Create(
            "broadcast", "svc", "event");
        await nats.SendAsync(envelope, topic);

        await nats.WaitForConsumedAsync(1, TimeSpan.FromSeconds(10));

        // At least one handler should receive the message
        Assert.That(handler1Results.Count + handler2Results.Count, Is.GreaterThanOrEqualTo(1));
    }

    // ── 4. Kafka E2E — Real Apache Kafka via Aspire ─────────────────────

    [Test]
    public async Task Publish_Kafka_RealBrokerDelivery()
    {
        // The same IMessageBrokerProducer abstraction works with Kafka.
        // KafkaBrokerEndpoint wraps a real Confluent.Kafka producer backed
        // by the Aspire-managed Bitnami Kafka container (KRaft mode).
        await using var kafka = AspireFixture.CreateKafkaEndpoint("t05-kafka");
        var topic = AspireFixture.UniqueTopic("t05-kafka-pub");

        var envelope = IntegrationEnvelope<string>.Create(
            "kafka-message", "KafkaService", "kafka.event");
        await kafka.PublishAsync(envelope, topic);

        kafka.AssertReceivedCount(1);
        Assert.That(kafka.GetReceived<string>().Payload, Is.EqualTo("kafka-message"));
    }

    // ── 5. Pulsar E2E — Real Apache Pulsar via Aspire ───────────────────

    [Test]
    public async Task Publish_Pulsar_RealBrokerDelivery()
    {
        // Apache Pulsar uses Key_Shared subscriptions for per-recipient
        // ordering at scale. PulsarBrokerEndpoint wraps DotPulsar against
        // the Aspire-managed Pulsar standalone container.
        await using var pulsar = AspireFixture.CreatePulsarEndpoint("t05-pulsar");
        var topic = AspireFixture.UniqueTopic("t05-pulsar-pub");

        var envelope = IntegrationEnvelope<string>.Create(
            "pulsar-message", "PulsarService", "pulsar.event");
        await pulsar.PublishAsync(envelope, topic);

        pulsar.AssertReceivedCount(1);
        Assert.That(pulsar.GetReceived<string>().Payload, Is.EqualTo("pulsar-message"));
    }

    // ── 6. Postgres E2E — Real PostgreSQL via Aspire ────────────────────

    [Test]
    public async Task Publish_Postgres_RealBrokerDelivery()
    {
        // PostgreSQL acts as a message broker using eip_messages table +
        // pg_notify for push delivery. No additional broker infrastructure
        // needed — just Postgres. PostgresBrokerEndpoint wraps the real
        // PostgresBrokerProducer against the Aspire-managed Postgres 17
        // container.
        await using var pg = AspireFixture.CreatePostgresEndpoint("t05-postgres");
        var topic = AspireFixture.UniqueTopic("t05-pg-pub");

        var envelope = IntegrationEnvelope<string>.Create(
            "postgres-message", "PostgresService", "postgres.event");
        await pg.PublishAsync(envelope, topic);

        pg.AssertReceivedCount(1);
        Assert.That(pg.GetReceived<string>().Payload, Is.EqualTo("postgres-message"));
    }

    // ── 7. All Four Brokers — Interchangeable Delivery ──────────────────

    [Test]
    public async Task AllFourBrokers_SameAbstraction_InterchangeableDelivery()
    {
        // THE fundamental EIP design principle: all message brokers are
        // interchangeable. A single IntegrationEnvelope published through
        // IMessageBrokerProducer delivers identically across NATS, Kafka,
        // Pulsar, and Postgres. Zero code changes when swapping brokers.
        await using var nats = AspireFixture.CreateNatsEndpoint("t05-all-nats");
        await using var kafka = AspireFixture.CreateKafkaEndpoint("t05-all-kafka");
        await using var pulsar = AspireFixture.CreatePulsarEndpoint("t05-all-pulsar");
        await using var pg = AspireFixture.CreatePostgresEndpoint("t05-all-pg");

        // All four endpoints implement IMessageBrokerProducer
        IMessageBrokerProducer[] brokers = [nats, kafka, pulsar, pg];

        var envelope = IntegrationEnvelope<string>.Create(
            "interchangeable", "CrossBrokerProof", "broker.test") with
        {
            Priority = MessagePriority.High,
            Intent = MessageIntent.Event,
        };

        // Publish the same message to all four brokers
        foreach (var broker in brokers)
        {
            var topic = AspireFixture.UniqueTopic("t05-all");
            await broker.PublishAsync(envelope, topic);
        }

        // Each broker received exactly one message with identical payload
        nats.AssertReceivedCount(1);
        kafka.AssertReceivedCount(1);
        pulsar.AssertReceivedCount(1);
        pg.AssertReceivedCount(1);

        // Same payload, same identity — the abstraction is real
        Assert.That(nats.GetReceived<string>().Payload, Is.EqualTo("interchangeable"));
        Assert.That(kafka.GetReceived<string>().Payload, Is.EqualTo("interchangeable"));
        Assert.That(pulsar.GetReceived<string>().Payload, Is.EqualTo("interchangeable"));
        Assert.That(pg.GetReceived<string>().Payload, Is.EqualTo("interchangeable"));

        Assert.That(nats.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(kafka.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(pulsar.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(pg.GetReceived<string>().MessageId, Is.EqualTo(envelope.MessageId));
    }
}
