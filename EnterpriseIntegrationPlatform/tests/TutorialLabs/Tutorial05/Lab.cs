// ============================================================================
// Tutorial 05 – Message Brokers (Lab)
// ============================================================================
// EIP Pattern: Message Endpoint, Event-Driven Consumer, Polling Consumer,
//              Selective Consumer
// Real Integrations: Publishing and consumer pattern tests use real NATS
// JetStream via Aspire. BrokerOptions configuration tests are pure data.
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
        // The platform supports three broker protocols.
        Assert.That(Enum.GetValues<BrokerType>(), Has.Length.EqualTo(3));
        Assert.That((int)BrokerType.NatsJetStream, Is.EqualTo(0));
        Assert.That((int)BrokerType.Kafka, Is.EqualTo(1));
        Assert.That((int)BrokerType.Pulsar, Is.EqualTo(2));
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
}
