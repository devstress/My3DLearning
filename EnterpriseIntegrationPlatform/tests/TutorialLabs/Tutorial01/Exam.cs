// ============================================================================
// Tutorial 01 – Introduction (Exam)
// ============================================================================
// EIP Pattern: Canonical Data Model
// End-to-End: Complex envelope scenarios through MockEndpoint — domain
// objects, causation chains, and record immutability verified at output.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;

namespace TutorialLabs.Tutorial01;

public sealed record OrderPayload(string OrderId, string Product, int Quantity, decimal Price);

[TestFixture]
public sealed class Exam
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
    public async Task EndToEnd_DomainObject_AllFieldsSurviveRoundTrip()
    {
        var order = new OrderPayload("ORD-001", "Widget", 5, 29.99m);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "OrderService", "order.created");

        await _output.PublishAsync(envelope, "orders");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<OrderPayload>();
        Assert.That(received.Payload.OrderId, Is.EqualTo("ORD-001"));
        Assert.That(received.Payload.Price, Is.EqualTo(29.99m));
        Assert.That(received.Source, Is.EqualTo("OrderService"));
        Assert.That(received.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task EndToEnd_CausationChain_PreservedThroughPipeline()
    {
        var messageA = IntegrationEnvelope<string>.Create(
            "PlaceOrder", "WebApp", "order.place") with
        {
            Intent = MessageIntent.Command,
        };

        var messageB = IntegrationEnvelope<string>.Create(
            "OrderPlaced", "OrderService", "order.placed",
            correlationId: messageA.CorrelationId,
            causationId: messageA.MessageId) with
        {
            Intent = MessageIntent.Event,
        };

        await _output.PublishAsync(messageA, "commands");
        await _output.PublishAsync(messageB, "events");

        _output.AssertReceivedCount(2);
        var receivedB = _output.GetReceived<string>(1);
        Assert.That(receivedB.CausationId, Is.EqualTo(messageA.MessageId));
        Assert.That(receivedB.CorrelationId, Is.EqualTo(messageA.CorrelationId));
        Assert.That(receivedB.MessageId, Is.Not.EqualTo(messageA.MessageId));
    }

    [Test]
    public async Task EndToEnd_ImmutableEnvelope_OriginalAndModifiedBothPreserved()
    {
        var original = IntegrationEnvelope<string>.Create(
            "original-payload", "TestService", "test.message");
        var modified = original with { Priority = MessagePriority.High };

        await _output.PublishAsync(original, "normal");
        await _output.PublishAsync(modified, "high");

        _output.AssertReceivedCount(2);
        var first = _output.GetReceived<string>(0);
        var second = _output.GetReceived<string>(1);
        Assert.That(first.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(second.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(first.MessageId, Is.EqualTo(second.MessageId));
    }
}
