// ============================================================================
// Tutorial 05 – Message Brokers (Lab)
// ============================================================================
// EIP Pattern: Message Endpoint
// End-to-End: Configure BrokerOptions for NATS/Kafka/Pulsar, send through
// MockEndpoint per broker type, verify abstraction works across protocols.
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
    public void SetUp()
    {
        _output = new MockEndpoint("output");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _output.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_NatsBrokerConfig_PublishToMockEndpoint()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.NatsJetStream,
            ConnectionString = "nats://localhost:15222",
        };

        var envelope = IntegrationEnvelope<string>.Create(
            "nats-message", "NatsService", "nats.event");

        await _output.PublishAsync(envelope, "nats-events");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("nats-message"));
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
    }

    [Test]
    public async Task EndToEnd_KafkaBrokerConfig_PublishToMockEndpoint()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.Kafka,
            ConnectionString = "localhost:9092",
        };

        var envelope = IntegrationEnvelope<string>.Create(
            "kafka-message", "KafkaService", "kafka.event");

        await _output.PublishAsync(envelope, "kafka-events");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("kafka-message"));
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Kafka));
    }

    [Test]
    public async Task EndToEnd_PulsarBrokerConfig_PublishToMockEndpoint()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.Pulsar,
            ConnectionString = "pulsar://localhost:6650",
        };

        var envelope = IntegrationEnvelope<string>.Create(
            "pulsar-message", "PulsarService", "pulsar.event");

        await _output.PublishAsync(envelope, "pulsar-events");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("pulsar-message"));
        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Pulsar));
    }

    [Test]
    public async Task EndToEnd_MultipleTopics_VerifyPerTopicDelivery()
    {
        var orderEnv = IntegrationEnvelope<string>.Create("order", "svc", "type");
        var paymentEnv = IntegrationEnvelope<string>.Create("payment", "svc", "type");
        var shippingEnv = IntegrationEnvelope<string>.Create("shipping", "svc", "type");

        await _output.PublishAsync(orderEnv, "orders-topic");
        await _output.PublishAsync(paymentEnv, "payments-topic");
        await _output.PublishAsync(shippingEnv, "shipping-topic");

        _output.AssertReceivedCount(3);
        _output.AssertReceivedOnTopic("orders-topic", 1);
        _output.AssertReceivedOnTopic("payments-topic", 1);
        _output.AssertReceivedOnTopic("shipping-topic", 1);
    }

    [Test]
    public async Task EndToEnd_EventDrivenConsumer_HandlerTriggered()
    {
        IntegrationEnvelope<string>? captured = null;
        await _output.StartAsync<string>("events", "group", msg =>
        {
            captured = msg;
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "event-driven", "EventSource", "event");
        await _output.SendAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("event-driven"));
    }

    [Test]
    public async Task EndToEnd_PollingConsumer_MessagesPolled()
    {
        var envelope1 = IntegrationEnvelope<string>.Create("poll-1", "svc", "type");
        var envelope2 = IntegrationEnvelope<string>.Create("poll-2", "svc", "type");
        await _output.SendAsync(envelope1);
        await _output.SendAsync(envelope2);

        var polled = await _output.PollAsync<string>("topic", "group", 10);

        Assert.That(polled, Has.Count.EqualTo(2));
        Assert.That(polled[0].Payload, Is.EqualTo("poll-1"));
        Assert.That(polled[1].Payload, Is.EqualTo("poll-2"));
    }
}
