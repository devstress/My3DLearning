using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessagingBridgeTests
{
    private IMessageBrokerConsumer _sourceConsumer = null!;
    private IMessageBrokerProducer _targetProducer = null!;
    private ILogger<MessagingBridge> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _sourceConsumer = Substitute.For<IMessageBrokerConsumer>();
        _targetProducer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<MessagingBridge>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sourceConsumer.DisposeAsync();
    }

    private MessagingBridge BuildBridge(MessagingBridgeOptions? options = null)
    {
        options ??= new MessagingBridgeOptions();
        return new MessagingBridge(_sourceConsumer, _targetProducer, Options.Create(options), _logger);
    }

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test", Guid? messageId = null)
    {
        var id = messageId ?? Guid.NewGuid();
        return new IntegrationEnvelope<string>
        {
            MessageId = id,
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "TestService",
            MessageType = "TestEvent",
            Payload = payload,
        };
    }

    [Test]
    public async Task StartAsync_ValidParams_SubscribesToSourceChannel()
    {
        var bridge = BuildBridge();

        await bridge.StartAsync<string>("source.topic", "target.topic", CancellationToken.None);

        await _sourceConsumer.Received(1).SubscribeAsync<string>(
            "source.topic",
            "messaging-bridge",
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_CustomConsumerGroup_UsesConfiguredGroup()
    {
        var bridge = BuildBridge(new MessagingBridgeOptions { ConsumerGroup = "custom-bridge" });

        await bridge.StartAsync<string>("src", "tgt", CancellationToken.None);

        await _sourceConsumer.Received(1).SubscribeAsync<string>(
            "src", "custom-bridge", Arg.Any<Func<IntegrationEnvelope<string>, Task>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_EmptySourceChannel_ThrowsArgumentException()
    {
        var bridge = BuildBridge();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await bridge.StartAsync<string>("", "target", CancellationToken.None));
    }

    [Test]
    public async Task StartAsync_EmptyTargetChannel_ThrowsArgumentException()
    {
        var bridge = BuildBridge();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await bridge.StartAsync<string>("source", "", CancellationToken.None));
    }

    [Test]
    public async Task Bridge_ForwardsMessageToTargetBroker()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        var envelope = BuildEnvelope("forwarded");
        await capturedHandler!(envelope);

        await _targetProducer.Received(1).PublishAsync(envelope, "target", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Bridge_ForwardedMessage_IncrementsForwardedCount()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);
        await capturedHandler!(BuildEnvelope());

        Assert.That(bridge.ForwardedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Bridge_DuplicateMessageId_SkipsSecondForward()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        var messageId = Guid.NewGuid();
        await capturedHandler!(BuildEnvelope("first", messageId));
        await capturedHandler!(BuildEnvelope("second", messageId));

        await _targetProducer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(), "target", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Bridge_DuplicateMessageId_IncrementsDuplicateCount()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        var messageId = Guid.NewGuid();
        await capturedHandler!(BuildEnvelope("first", messageId));
        await capturedHandler!(BuildEnvelope("second", messageId));

        Assert.That(bridge.DuplicateCount, Is.EqualTo(1));
        Assert.That(bridge.ForwardedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Bridge_UniqueMessages_AllForwarded()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        await capturedHandler!(BuildEnvelope("msg1"));
        await capturedHandler!(BuildEnvelope("msg2"));
        await capturedHandler!(BuildEnvelope("msg3"));

        Assert.That(bridge.ForwardedCount, Is.EqualTo(3));
        Assert.That(bridge.DuplicateCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Bridge_DeduplicationWindowExceeded_EvictsOldIds()
    {
        var bridge = BuildBridge(new MessagingBridgeOptions { DeduplicationWindowSize = 3 });
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        var firstId = Guid.NewGuid();
        await capturedHandler!(BuildEnvelope("first", firstId));
        await capturedHandler!(BuildEnvelope("second"));
        await capturedHandler!(BuildEnvelope("third"));
        await capturedHandler!(BuildEnvelope("fourth")); // evicts firstId

        // Re-send firstId — should now be treated as new since it was evicted
        await capturedHandler!(BuildEnvelope("first-again", firstId));

        Assert.That(bridge.ForwardedCount, Is.EqualTo(5));
        Assert.That(bridge.DuplicateCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Bridge_PreservesEnvelopeOnForward()
    {
        var bridge = BuildBridge();
        Func<IntegrationEnvelope<string>, Task>? capturedHandler = null;
        IntegrationEnvelope<string>? forwarded = null;

        await _sourceConsumer.SubscribeAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<IntegrationEnvelope<string>, Task>>(h => capturedHandler = h),
            Arg.Any<CancellationToken>());

        await _targetProducer.PublishAsync(
            Arg.Do<IntegrationEnvelope<string>>(e => forwarded = e),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await bridge.StartAsync<string>("source", "target", CancellationToken.None);

        var original = BuildEnvelope("preserved-payload");
        await capturedHandler!(original);

        Assert.That(forwarded, Is.Not.Null);
        Assert.That(forwarded!.MessageId, Is.EqualTo(original.MessageId));
        Assert.That(forwarded.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(forwarded.Payload, Is.EqualTo("preserved-payload"));
    }

    [Test]
    public async Task DisposeAsync_DisposesSourceConsumer()
    {
        var bridge = BuildBridge();

        await bridge.DisposeAsync();

        await _sourceConsumer.Received(1).DisposeAsync();
    }

    [Test]
    public void InitialCounts_AreZero()
    {
        var bridge = BuildBridge();

        Assert.That(bridge.ForwardedCount, Is.EqualTo(0));
        Assert.That(bridge.DuplicateCount, Is.EqualTo(0));
    }
}
