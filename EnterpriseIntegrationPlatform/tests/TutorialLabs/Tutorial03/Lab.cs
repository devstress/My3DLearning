// ============================================================================
// Tutorial 03 – Your First Message (Lab)
// ============================================================================
// This lab walks through the complete lifecycle of a message: creating an
// envelope, publishing it through a mocked broker, and consuming it on the
// other side.  NSubstitute is used so no real broker is needed.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial03;

// A simple domain payload used throughout this tutorial.
public sealed record OrderPayload(string OrderId, string Product, int Quantity);

[TestFixture]
public sealed class Lab
{
    // ── Creating Your First Envelope ────────────────────────────────────────

    [Test]
    public void CreateEnvelope_WithStringPayload_HasValidFields()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            payload: "Hello, Messaging!",
            source: "Tutorial03",
            messageType: "greeting");

        Assert.That(envelope.Payload, Is.EqualTo("Hello, Messaging!"));
        Assert.That(envelope.Source, Is.EqualTo("Tutorial03"));
        Assert.That(envelope.MessageType, Is.EqualTo("greeting"));
        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void CreateEnvelope_WithDomainObject_WrapsPayloadCorrectly()
    {
        var order = new OrderPayload("ORD-100", "Gadget", 3);

        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            payload: order,
            source: "OrderService",
            messageType: "order.created");

        Assert.That(envelope.Payload, Is.EqualTo(order));
        Assert.That(envelope.Payload.OrderId, Is.EqualTo("ORD-100"));
        Assert.That(envelope.Payload.Product, Is.EqualTo("Gadget"));
        Assert.That(envelope.Payload.Quantity, Is.EqualTo(3));
    }

    // ── Publishing with a Mocked Producer ───────────────────────────────────

    [Test]
    public async Task PublishAsync_WithMockedProducer_CallIsMade()
    {
        // Arrange: create a mock producer using NSubstitute.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var envelope = IntegrationEnvelope<string>.Create(
            "first-message", "Tutorial03", "demo.publish");

        // Act: publish the envelope to a topic.
        await producer.PublishAsync(envelope, "demo-topic");

        // Assert: verify the broker received exactly one publish call.
        await producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.Payload == "first-message"),
            Arg.Is("demo-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_WithOrderPayload_TopicIsCorrect()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var order = new OrderPayload("ORD-200", "Widget", 1);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "OrderService", "order.created");

        await producer.PublishAsync(envelope, "orders-topic");

        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<OrderPayload>>(),
            Arg.Is("orders-topic"),
            Arg.Any<CancellationToken>());
    }

    // ── Consuming with a Mocked Consumer ────────────────────────────────────

    [Test]
    public async Task SubscribeAsync_WhenHandlerInvoked_PayloadIsReceived()
    {
        // Arrange: configure the mock to capture the handler callback so we
        // can invoke it manually, simulating a broker delivering a message.
        var consumer = Substitute.For<IMessageBrokerConsumer>();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        consumer.SubscribeAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act: subscribe — this triggers the Arg.Do capture above.
        await consumer.SubscribeAsync<string>(
            "demo-topic",
            "demo-group",
            msg => Task.CompletedTask);

        // Create a message as if the broker delivered it.
        var envelope = IntegrationEnvelope<string>.Create(
            "consumed-payload", "Producer", "demo.event");

        Assert.That(capturedHandler, Is.Not.Null, "Handler should have been captured");

        // Simulate message delivery by invoking the captured handler.
        IntegrationEnvelope<string>? received = null;
        capturedHandler = msg =>
        {
            received = msg;
            return Task.CompletedTask;
        };
        await capturedHandler(envelope);

        // Assert: the handler processed the message.
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Payload, Is.EqualTo("consumed-payload"));
    }

    [Test]
    public async Task SubscribeAsync_MockVerification_SubscribeWasCalled()
    {
        var consumer = Substitute.For<IMessageBrokerConsumer>();

        await consumer.SubscribeAsync<string>(
            "events-topic",
            "my-consumer-group",
            _ => Task.CompletedTask);

        await consumer.Received(1).SubscribeAsync<string>(
            Arg.Is("events-topic"),
            Arg.Is("my-consumer-group"),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }
}
