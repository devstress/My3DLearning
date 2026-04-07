// ============================================================================
// Tutorial 05 – Message Brokers (Lab)
// ============================================================================
// EIP Pattern: Message Endpoint, Event-Driven Consumer, Polling Consumer,
//              Selective Consumer
// End-to-End: BrokerOptions configuration for NATS/Kafka/Pulsar, transaction
// timeout settings, event-driven vs polling vs selective consumer patterns,
// multi-topic delivery, and MockEndpoint as a protocol-agnostic broker
// abstraction.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("output");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

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
        // The platform supports three broker protocols.
        // Each has different delivery guarantees:
        // - NATS JetStream: no HOL blocking, at-least-once
        // - Kafka: event streaming, exactly-once semantics
        // - Pulsar: Key_Shared per-recipient ordering
        Assert.That(Enum.GetValues<BrokerType>(), Has.Length.EqualTo(3));
        Assert.That((int)BrokerType.NatsJetStream, Is.EqualTo(0));
        Assert.That((int)BrokerType.Kafka, Is.EqualTo(1));
        Assert.That((int)BrokerType.Pulsar, Is.EqualTo(2));
    }

    // ── 2. Protocol-Agnostic Publishing ─────────────────────────────────

    [Test]
    public async Task Publish_NatsConfig_MessageDeliveredViaAbstraction()
    {
        // IMessageBrokerProducer abstracts away the protocol.
        // The same PublishAsync call works for NATS, Kafka, or Pulsar —
        // MockEndpoint stands in for any real broker implementation.
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.NatsJetStream,
            ConnectionString = "nats://localhost:15222",
        };

        var envelope = IntegrationEnvelope<string>.Create(
            "nats-message", "NatsService", "nats.event");
        await _output.PublishAsync(envelope, "nats-events");

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("nats-message"));
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
    }

    [Test]
    public async Task Publish_MultipleTopics_PerTopicDeliveryVerified()
    {
        // A single broker endpoint routes messages to different topics.
        // Topic-level isolation ensures consumers see only their messages.
        var orderEnv = IntegrationEnvelope<string>.Create("order", "svc", "order.created");
        var paymentEnv = IntegrationEnvelope<string>.Create("payment", "svc", "payment.processed");
        var shippingEnv = IntegrationEnvelope<string>.Create("shipping", "svc", "shipment.dispatched");

        await _output.PublishAsync(orderEnv, "orders-topic");
        await _output.PublishAsync(paymentEnv, "payments-topic");
        await _output.PublishAsync(shippingEnv, "shipping-topic");

        _output.AssertReceivedCount(3);
        _output.AssertReceivedOnTopic("orders-topic", 1);
        _output.AssertReceivedOnTopic("payments-topic", 1);
        _output.AssertReceivedOnTopic("shipping-topic", 1);
        Assert.That(_output.GetReceivedTopics(), Has.Count.EqualTo(3));
    }

    // ── 3. Consumer Patterns ────────────────────────────────────────────

    [Test]
    public async Task EventDrivenConsumer_HandlerTriggeredOnMessageArrival()
    {
        // IEventDrivenConsumer.StartAsync registers a push-based handler.
        // The broker calls the handler for each arriving message — no polling.
        IntegrationEnvelope<string>? captured = null;
        await _output.StartAsync<string>("events", "group", msg =>
        {
            captured = msg;
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "event-driven", "EventSource", "event.fired");
        await _output.SendAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("event-driven"));
        Assert.That(captured.Source, Is.EqualTo("EventSource"));
    }

    [Test]
    public async Task PollingConsumer_BatchRetrieval_MaxMessagesRespected()
    {
        // IPollingConsumer.PollAsync retrieves up to maxMessages from queue.
        // The consumer controls when to fetch — useful for batch processing.
        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"batch-{i}", "svc", "type");
            await _output.SendAsync(env);
        }

        var polled = await _output.PollAsync<string>("topic", "group", maxMessages: 3);

        Assert.That(polled, Has.Count.EqualTo(3));
        Assert.That(polled[0].Payload, Is.EqualTo("batch-0"));
        Assert.That(polled[2].Payload, Is.EqualTo("batch-2"));
    }

    [Test]
    public async Task SelectiveConsumer_PredicateFilters_OnlyMatchingDelivered()
    {
        // ISelectiveConsumer adds a predicate gate before the handler.
        // Messages that don't match the predicate are silently skipped.
        var delivered = new List<string>();
        await _output.SubscribeAsync<string>("orders", "group",
            env => env.Priority >= MessagePriority.High,
            msg =>
            {
                delivered.Add(msg.Payload);
                return Task.CompletedTask;
            });

        var high = IntegrationEnvelope<string>.Create(
            "urgent", "svc", "order") with { Priority = MessagePriority.High };
        var low = IntegrationEnvelope<string>.Create(
            "routine", "svc", "order") with { Priority = MessagePriority.Low };
        var critical = IntegrationEnvelope<string>.Create(
            "emergency", "svc", "order") with { Priority = MessagePriority.Critical };

        await _output.SendAsync(high);
        await _output.SendAsync(low);
        await _output.SendAsync(critical);

        Assert.That(delivered, Has.Count.EqualTo(2));
        Assert.That(delivered, Does.Contain("urgent"));
        Assert.That(delivered, Does.Contain("emergency"));
    }

    [Test]
    public async Task SubscribeConsumer_MultipleHandlers_AllInvoked()
    {
        // Multiple SubscribeAsync calls register independent handlers.
        // Each handler receives the same message — fan-out to local handlers.
        var handler1Results = new List<string>();
        var handler2Results = new List<string>();

        await _output.SubscribeAsync<string>("events", "group-1", msg =>
        {
            handler1Results.Add(msg.Payload);
            return Task.CompletedTask;
        });
        await _output.SubscribeAsync<string>("events", "group-2", msg =>
        {
            handler2Results.Add(msg.Payload);
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "broadcast", "svc", "event");
        await _output.SendAsync(envelope);

        Assert.That(handler1Results, Has.Count.EqualTo(1));
        Assert.That(handler2Results, Has.Count.EqualTo(1));
        Assert.That(handler1Results[0], Is.EqualTo("broadcast"));
    }
}
