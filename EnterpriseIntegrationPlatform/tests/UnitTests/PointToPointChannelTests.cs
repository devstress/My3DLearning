using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PointToPointChannelTests
{
    private IMessageBrokerProducer _producer = null!;
    private IMessageBrokerConsumer _consumer = null!;
    private ILogger<PointToPointChannel> _logger = null!;
    private PointToPointChannel _channel = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _logger = Substitute.For<ILogger<PointToPointChannel>>();
        _channel = new PointToPointChannel(_producer, _consumer, _logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test")
        => IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    [Test]
    public async Task SendAsync_ValidEnvelope_PublishesToBroker()
    {
        var envelope = BuildEnvelope();

        await _channel.SendAsync(envelope, "orders.queue", CancellationToken.None);

        await _producer.Received(1).PublishAsync(envelope, "orders.queue", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _channel.SendAsync<string>(null!, "topic", CancellationToken.None));
    }

    [Test]
    public async Task SendAsync_EmptyChannel_ThrowsArgumentException()
    {
        var envelope = BuildEnvelope();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.SendAsync(envelope, "", CancellationToken.None));
    }

    [Test]
    public async Task ReceiveAsync_ValidParams_SubscribesWithConsumerGroup()
    {
        Func<IntegrationEnvelope<string>, Task> handler = _ => Task.CompletedTask;

        await _channel.ReceiveAsync("orders.queue", "order-processors", handler, CancellationToken.None);

        await _consumer.Received(1).SubscribeAsync(
            "orders.queue", "order-processors", handler, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReceiveAsync_EmptyChannel_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.ReceiveAsync<string>("", "group", _ => Task.CompletedTask, CancellationToken.None));
    }

    [Test]
    public async Task ReceiveAsync_EmptyConsumerGroup_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _channel.ReceiveAsync<string>("topic", "", _ => Task.CompletedTask, CancellationToken.None));
    }

    [Test]
    public async Task ReceiveAsync_NullHandler_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _channel.ReceiveAsync<string>("topic", "group", null!, CancellationToken.None));
    }
}
