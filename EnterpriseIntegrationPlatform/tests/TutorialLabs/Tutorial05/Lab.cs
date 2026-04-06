// ============================================================================
// Tutorial 05 – Message Brokers (Lab)
// ============================================================================
// This lab explores the three supported message broker implementations
// (NATS JetStream, Kafka, Pulsar) through BrokerOptions configuration and
// mocked producers.  You will configure each broker, publish messages to
// specific topics, and verify the interactions.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Lab
{
    // ── Configuring BrokerOptions for Each Broker ───────────────────────────

    [Test]
    public void BrokerOptions_ConfiguredForNats()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.NatsJetStream,
            ConnectionString = "nats://localhost:15222",
            TransactionTimeoutSeconds = 30,
        };

        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
        Assert.That(options.ConnectionString, Is.EqualTo("nats://localhost:15222"));
        Assert.That(options.TransactionTimeoutSeconds, Is.EqualTo(30));
    }

    [Test]
    public void BrokerOptions_ConfiguredForKafka()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.Kafka,
            ConnectionString = "localhost:9092",
            TransactionTimeoutSeconds = 60,
        };

        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Kafka));
        Assert.That(options.ConnectionString, Is.EqualTo("localhost:9092"));
        Assert.That(options.TransactionTimeoutSeconds, Is.EqualTo(60));
    }

    [Test]
    public void BrokerOptions_ConfiguredForPulsar()
    {
        var options = new BrokerOptions
        {
            BrokerType = BrokerType.Pulsar,
            ConnectionString = "pulsar://localhost:6650",
            TransactionTimeoutSeconds = 45,
        };

        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.Pulsar));
        Assert.That(options.ConnectionString, Is.EqualTo("pulsar://localhost:6650"));
        Assert.That(options.TransactionTimeoutSeconds, Is.EqualTo(45));
    }

    // ── Publishing Through Mocked Producers ─────────────────────────────────

    [Test]
    public async Task Publish_WithNatsProducer_VerifyTopicAndPayload()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "nats-message", "NatsService", "nats.event");

        await producer.PublishAsync(envelope, "nats-events");

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "nats-message"),
            Arg.Is("nats-events"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WithKafkaProducer_VerifyTopicAndPayload()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "kafka-message", "KafkaService", "kafka.event");

        await producer.PublishAsync(envelope, "kafka-events");

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "kafka-message"),
            Arg.Is("kafka-events"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WithPulsarProducer_VerifyTopicAndPayload()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "pulsar-message", "PulsarService", "pulsar.event");

        await producer.PublishAsync(envelope, "pulsar-events");

        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "pulsar-message"),
            Arg.Is("pulsar-events"),
            Arg.Any<CancellationToken>());
    }

    // ── Multiple Topics ─────────────────────────────────────────────────────

    [Test]
    public async Task Publish_MultipleTopics_EachReceivesCorrectMessage()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var orderEnvelope = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");

        var paymentEnvelope = IntegrationEnvelope<string>.Create(
            "payment-received", "PaymentService", "payment.received");

        var shippingEnvelope = IntegrationEnvelope<string>.Create(
            "shipment-dispatched", "ShippingService", "shipment.dispatched");

        await producer.PublishAsync(orderEnvelope, "orders-topic");
        await producer.PublishAsync(paymentEnvelope, "payments-topic");
        await producer.PublishAsync(shippingEnvelope, "shipping-topic");

        // Verify each topic got exactly one message.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("orders-topic"),
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("payments-topic"),
            Arg.Any<CancellationToken>());

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("shipping-topic"),
            Arg.Any<CancellationToken>());

        // Total publish calls = 3.
        await producer.Received(3).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
