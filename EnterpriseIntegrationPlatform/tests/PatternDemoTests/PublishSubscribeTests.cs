using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Publish-Subscribe Channel pattern.
/// Broadcasts a message to all registered subscribers.
/// BizTalk equivalent: MessageBox subscription model — multiple Send Ports
/// and Orchestrations subscribe to the same message type.
/// EIP: Publish-Subscribe Channel (p. 106)
/// </summary>
public class PublishSubscribeTests
{
    private record NotificationPayload(string EventType, string Message);

    [Fact]
    public async Task Delivers_ToAll_Subscribers()
    {
        var channel = new PublishSubscribeChannel<NotificationPayload>();
        var received = new List<string>();

        channel.Subscribe(async (env, ct) => received.Add("email-service"));
        channel.Subscribe(async (env, ct) => received.Add("sms-service"));
        channel.Subscribe(async (env, ct) => received.Add("push-service"));

        var envelope = IntegrationEnvelope<NotificationPayload>.Create(
            new NotificationPayload("OrderShipped", "Your order has shipped!"),
            "OrderService", "Notification");

        await channel.PublishAsync(envelope);

        received.Should().HaveCount(3);
        received.Should().Contain("email-service");
        received.Should().Contain("sms-service");
        received.Should().Contain("push-service");
    }

    [Fact]
    public async Task Unsubscribed_Handler_StopsReceiving()
    {
        var channel = new PublishSubscribeChannel<NotificationPayload>();
        var received = new List<string>();

        var sub1 = channel.Subscribe(async (env, ct) => received.Add("active"));
        var sub2 = channel.Subscribe(async (env, ct) => received.Add("removed"));

        channel.SubscriberCount.Should().Be(2);

        sub2.Dispose(); // Unsubscribe

        channel.SubscriberCount.Should().Be(1);

        var envelope = IntegrationEnvelope<NotificationPayload>.Create(
            new NotificationPayload("Test", "hello"), "Test", "Notification");

        await channel.PublishAsync(envelope);

        received.Should().ContainSingle().Which.Should().Be("active");
    }
}
