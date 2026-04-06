// ============================================================================
// Tutorial 06 – Messaging Channels (Exam)
// ============================================================================
// E2E challenges: messaging bridge via Point-to-Point, pub-sub fan-out with
// verification, and type-based routing with multiple message types.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial06;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_Bridge_PointToPointRelay()
    {
        await using var source = new MockEndpoint("source");
        await using var target = new MockEndpoint("target");

        var inbound = new PointToPointChannel(
            source, source, NullLogger<PointToPointChannel>.Instance);
        var outbound = new PointToPointChannel(
            target, target, NullLogger<PointToPointChannel>.Instance);

        await inbound.ReceiveAsync<string>("inbound-q", "bridge-group",
            async msg => await outbound.SendAsync(msg, "outbound-q", CancellationToken.None),
            CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create(
            "bridged-payload", "SourceSystem", "source.event");
        await source.SendAsync(envelope);

        target.AssertReceivedCount(1);
        Assert.That(target.GetReceived<string>().Payload, Is.EqualTo("bridged-payload"));
        target.AssertReceivedOnTopic("outbound-q", 1);
    }

    [Test]
    public async Task Challenge2_PubSubFanOut_ThreeSubscribersReceive()
    {
        await using var endpoint = new MockEndpoint("fanout");
        var channel = new PublishSubscribeChannel(
            endpoint, endpoint, NullLogger<PublishSubscribeChannel>.Instance);

        var results = new List<string>();
        await channel.SubscribeAsync<string>("notifications", "email",
            msg => { results.Add("email"); return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "sms",
            msg => { results.Add("sms"); return Task.CompletedTask; }, CancellationToken.None);
        await channel.SubscribeAsync<string>("notifications", "push",
            msg => { results.Add("push"); return Task.CompletedTask; }, CancellationToken.None);

        var envelope = IntegrationEnvelope<string>.Create(
            "broadcast", "NotificationService", "notification.sent");
        await endpoint.SendAsync(envelope);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results, Does.Contain("email"));
        Assert.That(results, Does.Contain("sms"));
        Assert.That(results, Does.Contain("push"));
    }

    [Test]
    public async Task Challenge3_DatatypeChannel_MultipleTypesRoutedCorrectly()
    {
        await using var endpoint = new MockEndpoint("datatype");
        var options = Options.Create(new DatatypeChannelOptions
            { TopicPrefix = "dt", Separator = "." });
        var channel = new DatatypeChannel(
            endpoint, options, NullLogger<DatatypeChannel>.Instance);

        var orderEnv = IntegrationEnvelope<string>.Create("o1", "svc", "order.created");
        var paymentEnv = IntegrationEnvelope<string>.Create("p1", "svc", "payment.received");
        var inventoryEnv = IntegrationEnvelope<string>.Create("i1", "svc", "inventory.updated");

        await channel.PublishAsync(orderEnv, CancellationToken.None);
        await channel.PublishAsync(paymentEnv, CancellationToken.None);
        await channel.PublishAsync(inventoryEnv, CancellationToken.None);

        endpoint.AssertReceivedCount(3);
        endpoint.AssertReceivedOnTopic("dt.order.created", 1);
        endpoint.AssertReceivedOnTopic("dt.payment.received", 1);
        endpoint.AssertReceivedOnTopic("dt.inventory.updated", 1);
        Assert.That(endpoint.GetReceivedTopics(), Has.Count.EqualTo(3));
    }
}
