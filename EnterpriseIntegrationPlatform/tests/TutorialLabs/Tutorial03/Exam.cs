// ============================================================================
// Tutorial 03 – First Message (Exam)
// ============================================================================
// EIP Pattern: Message Channel
// End-to-End: PointToPointChannel and PublishSubscribeChannel with
// MockEndpoints — real channel components delivering real messages.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial03;

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
    public async Task EndToEnd_PointToPointChannel_DeliversMessage()
    {
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "p2p-delivery", "OrderService", "order.created");

        await channel.SendAsync(envelope, "orders-queue", CancellationToken.None);

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("p2p-delivery"));
        Assert.That(received.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task EndToEnd_PublishSubscribeChannel_FanOutToSubscribers()
    {
        var sub1 = new MockEndpoint("subscriber-1");
        var sub2 = new MockEndpoint("subscriber-2");

        var channel1 = new PublishSubscribeChannel(
            sub1, sub1, NullLogger<PublishSubscribeChannel>.Instance);
        var channel2 = new PublishSubscribeChannel(
            sub2, sub2, NullLogger<PublishSubscribeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "fanout-event", "EventService", "event.fired");

        await channel1.PublishAsync(envelope, "events", CancellationToken.None);
        await channel2.PublishAsync(envelope, "events", CancellationToken.None);

        sub1.AssertReceivedCount(1);
        sub2.AssertReceivedCount(1);
        Assert.That(sub1.GetReceived<string>().Payload, Is.EqualTo("fanout-event"));
        Assert.That(sub2.GetReceived<string>().Payload, Is.EqualTo("fanout-event"));

        await sub1.DisposeAsync();
        await sub2.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_MultiTopicRouting_VerifyTopicCounts()
    {
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"order-{i}", "svc", "type");
            await channel.SendAsync(env, "orders", CancellationToken.None);
        }
        for (var i = 0; i < 2; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"payment-{i}", "svc", "type");
            await channel.SendAsync(env, "payments", CancellationToken.None);
        }

        _output.AssertReceivedCount(5);
        _output.AssertReceivedOnTopic("orders", 3);
        _output.AssertReceivedOnTopic("payments", 2);
    }
}
