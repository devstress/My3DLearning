using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PublishSubscribeChannelTests
{
    private IMessageBrokerProducer _producer = null!;
    private IMessageBrokerConsumer _consumer = null!;
    private ILogger<PublishSubscribeChannel> _logger = null!;
    private PublishSubscribeChannel _channel = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _logger = Substitute.For<ILogger<PublishSubscribeChannel>>();
        _channel = new PublishSubscribeChannel(_producer, _consumer, _logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test")
        => IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    [Test]
    public async Task PublishAsync_ValidEnvelope_PublishesToBroker()
    {
        var envelope = BuildEnvelope();

        await _channel.PublishAsync(envelope, "events.topic", CancellationToken.None);

        await _producer.Received(1).PublishAsync(envelope, "events.topic", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _channel.PublishAsync<string>(null!, "topic", CancellationToken.None));
    }

    [Test]
    public async Task PublishAsync_EmptyChannel_ThrowsArgumentException()
    {
        var envelope = BuildEnvelope();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.PublishAsync(envelope, "", CancellationToken.None));
    }

    [Test]
    public async Task SubscribeAsync_ValidParams_SubscribesWithUniqueConsumerGroup()
    {
        Func<IntegrationEnvelope<string>, Task> handler = _ => Task.CompletedTask;

        await _channel.SubscribeAsync("events.topic", "subscriber-A", handler, CancellationToken.None);

        await _consumer.Received(1).SubscribeAsync(
            "events.topic",
            "pubsub-events.topic-subscriber-A",
            handler,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubscribeAsync_DifferentSubscribers_GetDifferentConsumerGroups()
    {
        Func<IntegrationEnvelope<string>, Task> handler = _ => Task.CompletedTask;

        await _channel.SubscribeAsync("events", "sub-1", handler, CancellationToken.None);
        await _channel.SubscribeAsync("events", "sub-2", handler, CancellationToken.None);

        await _consumer.Received(1).SubscribeAsync(
            "events", "pubsub-events-sub-1", handler, Arg.Any<CancellationToken>());
        await _consumer.Received(1).SubscribeAsync(
            "events", "pubsub-events-sub-2", handler, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubscribeAsync_EmptyChannel_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.SubscribeAsync<string>("", "sub", _ => Task.CompletedTask, CancellationToken.None));
    }

    [Test]
    public async Task SubscribeAsync_EmptySubscriberId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.SubscribeAsync<string>("topic", "", _ => Task.CompletedTask, CancellationToken.None));
    }

    [Test]
    public async Task SubscribeAsync_NullHandler_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _channel.SubscribeAsync<string>("topic", "sub", null!, CancellationToken.None));
    }
}
