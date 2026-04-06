// ============================================================================
// Tutorial 03 – First Message (Lab)
// ============================================================================
// EIP Pattern: Message Channel
// End-to-End: Use MockEndpoint as producer/consumer, send and receive
// messages, verify end-to-end delivery.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;

namespace TutorialLabs.Tutorial03;

public sealed record OrderPayload(string OrderId, string Product, int Quantity);

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
    public async Task EndToEnd_PublishStringMessage_ReceivedAtOutput()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello, Messaging!", "Tutorial03", "greeting");

        await _output.PublishAsync(envelope, "greetings");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("Hello, Messaging!"));
        Assert.That(received.Source, Is.EqualTo("Tutorial03"));
    }

    [Test]
    public async Task EndToEnd_PublishDomainObject_PayloadPreserved()
    {
        var order = new OrderPayload("ORD-100", "Gadget", 3);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "OrderService", "order.created");

        await _output.PublishAsync(envelope, "orders");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<OrderPayload>();
        Assert.That(received.Payload.OrderId, Is.EqualTo("ORD-100"));
        Assert.That(received.Payload.Product, Is.EqualTo("Gadget"));
    }

    [Test]
    public async Task EndToEnd_SubscribeAndSend_HandlerInvoked()
    {
        IntegrationEnvelope<string>? captured = null;
        await _output.SubscribeAsync<string>("topic", "group", msg =>
        {
            captured = msg;
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "consumed-payload", "Producer", "demo.event");
        await _output.SendAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("consumed-payload"));
    }

    [Test]
    public async Task EndToEnd_MultipleMessages_AllCaptured()
    {
        for (var i = 0; i < 3; i++)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"msg-{i}", "source", "type");
            await _output.PublishAsync(envelope, "topic");
        }

        _output.AssertReceivedCount(3);
        Assert.That(_output.GetReceived<string>(0).Payload, Is.EqualTo("msg-0"));
        Assert.That(_output.GetReceived<string>(2).Payload, Is.EqualTo("msg-2"));
    }

    [Test]
    public async Task EndToEnd_TopicRouting_MessagesOnCorrectTopics()
    {
        var orderEnv = IntegrationEnvelope<string>.Create("order", "svc", "type");
        var paymentEnv = IntegrationEnvelope<string>.Create("payment", "svc", "type");

        await _output.PublishAsync(orderEnv, "orders-topic");
        await _output.PublishAsync(paymentEnv, "payments-topic");

        _output.AssertReceivedOnTopic("orders-topic", 1);
        _output.AssertReceivedOnTopic("payments-topic", 1);
        Assert.That(_output.GetReceivedTopics(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task EndToEnd_SendAndReceive_FullRoundTrip()
    {
        IntegrationEnvelope<string>? received = null;
        await _output.SubscribeAsync<string>("channel", "group", msg =>
        {
            received = msg;
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "round-trip", "Producer", "test");
        await _output.SendAsync(envelope);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(received.Payload, Is.EqualTo("round-trip"));
    }
}
