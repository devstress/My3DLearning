using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace UnitTests;

/// <summary>
/// Tests for the Detour EIP pattern in Processing.Routing.
/// </summary>
[TestFixture]
public sealed class DetourTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<Detour> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<Detour>>();
    }

    [Test]
    public async Task RouteAsync_DetourEnabled_RoutesToDetourTopic()
    {
        var options = CreateOptions(enabledAtStartup: true);
        var sut = new Detour(_producer, options, _logger);

        var envelope = CreateEnvelope("data");

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("detour-topic"));

        await _producer.Received(1).PublishAsync(
            Arg.Is(envelope),
            Arg.Is("detour-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_DetourDisabled_RoutesToOutputTopic()
    {
        var options = CreateOptions(enabledAtStartup: false);
        var sut = new Detour(_producer, options, _logger);

        var envelope = CreateEnvelope("data");

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.False);
        Assert.That(result.TargetTopic, Is.EqualTo("output-topic"));

        await _producer.Received(1).PublishAsync(
            Arg.Is(envelope),
            Arg.Is("output-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_PerMessageDetour_ActivatesDetour()
    {
        var options = CreateOptions(enabledAtStartup: false, detourMetadataKey: "debug");
        var sut = new Detour(_producer, options, _logger);

        var envelope = CreateEnvelope("data");
        envelope.Metadata["debug"] = "true";

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("detour-topic"));
    }

    [Test]
    public async Task RouteAsync_PerMessageDetourNotSet_RoutesToOutput()
    {
        var options = CreateOptions(enabledAtStartup: false, detourMetadataKey: "debug");
        var sut = new Detour(_producer, options, _logger);

        var envelope = CreateEnvelope("data");
        // No "debug" metadata key set

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.False);
        Assert.That(result.TargetTopic, Is.EqualTo("output-topic"));
    }

    [Test]
    public void SetEnabled_TogglesGlobalDetour()
    {
        var options = CreateOptions(enabledAtStartup: false);
        var sut = new Detour(_producer, options, _logger);

        Assert.That(sut.IsEnabled, Is.False);

        sut.SetEnabled(true);
        Assert.That(sut.IsEnabled, Is.True);

        sut.SetEnabled(false);
        Assert.That(sut.IsEnabled, Is.False);
    }

    [Test]
    public void RouteAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var options = CreateOptions();
        var sut = new Detour(_producer, options, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.RouteAsync<string>(null!));
    }

    private static IOptions<DetourOptions> CreateOptions(
        bool enabledAtStartup = false,
        string? detourMetadataKey = null) =>
        Options.Create(new DetourOptions
        {
            DetourTopic = "detour-topic",
            OutputTopic = "output-topic",
            EnabledAtStartup = enabledAtStartup,
            DetourMetadataKey = detourMetadataKey
        });

    private static IntegrationEnvelope<string> CreateEnvelope(string payload) =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "test.message");
}

/// <summary>
/// Tests for the Message History helper in Contracts.
/// </summary>
[TestFixture]
public sealed class MessageHistoryTests
{
    [Test]
    public void AppendHistory_AddsEntryToMetadata()
    {
        var envelope = CreateEnvelope("data");

        MessageHistoryHelper.AppendHistory(envelope, "Router", MessageHistoryStatus.Completed);

        var history = MessageHistoryHelper.GetHistory(envelope);

        Assert.That(history, Has.Count.EqualTo(1));
        Assert.That(history[0].ActivityName, Is.EqualTo("Router"));
        Assert.That(history[0].Status, Is.EqualTo(MessageHistoryStatus.Completed));
    }

    [Test]
    public void AppendHistory_MultipleSteps_PreservesChain()
    {
        var envelope = CreateEnvelope("data");

        MessageHistoryHelper.AppendHistory(envelope, "Ingestion", MessageHistoryStatus.Completed);
        MessageHistoryHelper.AppendHistory(envelope, "Transform", MessageHistoryStatus.Completed);
        MessageHistoryHelper.AppendHistory(envelope, "Routing", MessageHistoryStatus.Failed, "No match");

        var history = MessageHistoryHelper.GetHistory(envelope);

        Assert.That(history, Has.Count.EqualTo(3));
        Assert.That(history[0].ActivityName, Is.EqualTo("Ingestion"));
        Assert.That(history[1].ActivityName, Is.EqualTo("Transform"));
        Assert.That(history[2].ActivityName, Is.EqualTo("Routing"));
        Assert.That(history[2].Status, Is.EqualTo(MessageHistoryStatus.Failed));
        Assert.That(history[2].Detail, Is.EqualTo("No match"));
    }

    [Test]
    public void GetHistory_NoHistory_ReturnsEmpty()
    {
        var envelope = CreateEnvelope("data");

        var history = MessageHistoryHelper.GetHistory(envelope);

        Assert.That(history, Is.Empty);
    }

    [Test]
    public void GetHistory_InvalidJson_ReturnsEmpty()
    {
        var envelope = CreateEnvelope("data");
        envelope.Metadata[MessageHistoryHelper.MetadataKey] = "not-valid-json";

        var history = MessageHistoryHelper.GetHistory(envelope);

        Assert.That(history, Is.Empty);
    }

    [Test]
    public void AppendHistory_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MessageHistoryHelper.AppendHistory<string>(null!, "Step", MessageHistoryStatus.Completed));
    }

    [Test]
    public void AppendHistory_NullActivityName_ThrowsArgumentException()
    {
        var envelope = CreateEnvelope("data");

        Assert.Throws<ArgumentNullException>(() =>
            MessageHistoryHelper.AppendHistory(envelope, null!, MessageHistoryStatus.Completed));
    }

    private static IntegrationEnvelope<string> CreateEnvelope(string payload) =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "test.message");
}

/// <summary>
/// Tests for the Test Message Generator system management pattern.
/// </summary>
[TestFixture]
public sealed class TestMessageGeneratorTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<TestMessageGenerator> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<TestMessageGenerator>>();
    }

    [Test]
    public async Task GenerateAsync_PublishesTestMessage()
    {
        var sut = new TestMessageGenerator(_producer, _logger);

        var result = await sut.GenerateAsync("test-topic");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("test-topic"));
        Assert.That(result.MessageId, Is.Not.EqualTo(Guid.Empty));

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.Metadata.ContainsKey(TestMessageGenerator.TestMessageMetadataKey) &&
                e.MessageType == TestMessageGenerator.TestMessageType),
            Arg.Is("test-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GenerateAsync_CustomPayload_PublishesWithPayload()
    {
        var sut = new TestMessageGenerator(_producer, _logger);

        var result = await sut.GenerateAsync(42, "number-topic");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("number-topic"));

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<int>>(e => e.Payload == 42),
            Arg.Is("number-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GenerateAsync_ProducerFails_ReturnsFailure()
    {
        _producer.PublishAsync(
                Arg.Any<IntegrationEnvelope<string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        var sut = new TestMessageGenerator(_producer, _logger);

        var result = await sut.GenerateAsync("test-topic");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Broker unavailable"));
    }

    [Test]
    public void GenerateAsync_NullTopic_ThrowsArgumentException()
    {
        var sut = new TestMessageGenerator(_producer, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.GenerateAsync(null!));
    }
}

/// <summary>
/// Tests for the Channel Purger system management pattern.
/// </summary>
[TestFixture]
public sealed class ChannelPurgerTests
{
    private IMessageBrokerConsumer _consumer = null!;
    private ILogger<ChannelPurger> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _logger = Substitute.For<ILogger<ChannelPurger>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    [Test]
    public async Task PurgeAsync_DrainsCancelledGracefully_ReturnsSuccess()
    {
        // Simulate subscribe that completes (drain timeout causes cancel)
        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<object>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ChannelPurger(_consumer, _logger, TimeSpan.FromMilliseconds(100));

        var result = await sut.PurgeAsync("stale-topic", "purger-group");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Topic, Is.EqualTo("stale-topic"));
    }

    [Test]
    public async Task PurgeAsync_ConsumerThrows_ReturnsFailure()
    {
        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<object>, Task>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker error"));

        var sut = new ChannelPurger(_consumer, _logger, TimeSpan.FromMilliseconds(100));

        var result = await sut.PurgeAsync("stale-topic", "purger-group");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Broker error"));
    }

    [Test]
    public void PurgeAsync_NullTopic_ThrowsArgumentException()
    {
        var sut = new ChannelPurger(_consumer, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.PurgeAsync(null!, "group"));
    }
}

/// <summary>
/// Tests for the Smart Proxy system management pattern.
/// </summary>
[TestFixture]
public sealed class SmartProxyTests
{
    private ILogger<SmartProxy> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<SmartProxy>>();
    }

    [Test]
    public void TrackRequest_WithReplyTo_ReturnsTrue()
    {
        var sut = new SmartProxy(_logger);
        var request = CreateEnvelope("request", replyTo: "requester-reply-topic");

        var tracked = sut.TrackRequest(request);

        Assert.That(tracked, Is.True);
        Assert.That(sut.OutstandingCount, Is.EqualTo(1));
    }

    [Test]
    public void TrackRequest_NoReplyTo_ReturnsFalse()
    {
        var sut = new SmartProxy(_logger);
        var request = CreateEnvelope("request");

        var tracked = sut.TrackRequest(request);

        Assert.That(tracked, Is.False);
        Assert.That(sut.OutstandingCount, Is.EqualTo(0));
    }

    [Test]
    public void TrackRequest_DuplicateCorrelation_ReturnsFalse()
    {
        var sut = new SmartProxy(_logger);
        var correlationId = Guid.NewGuid();
        var request1 = CreateEnvelope("req1", replyTo: "topic-a", correlationId: correlationId);
        var request2 = CreateEnvelope("req2", replyTo: "topic-b", correlationId: correlationId);

        Assert.That(sut.TrackRequest(request1), Is.True);
        Assert.That(sut.TrackRequest(request2), Is.False);
        Assert.That(sut.OutstandingCount, Is.EqualTo(1));
    }

    [Test]
    public void CorrelateReply_MatchingCorrelation_ReturnsCorrelation()
    {
        var sut = new SmartProxy(_logger);
        var correlationId = Guid.NewGuid();
        var request = CreateEnvelope("request", replyTo: "requester-reply", correlationId: correlationId);
        var reply = CreateEnvelope("response", correlationId: correlationId);

        sut.TrackRequest(request);
        var correlation = sut.CorrelateReply(reply);

        Assert.That(correlation, Is.Not.Null);
        Assert.That(correlation!.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(correlation.OriginalReplyTo, Is.EqualTo("requester-reply"));
        Assert.That(correlation.RequestMessageId, Is.EqualTo(request.MessageId));
        Assert.That(sut.OutstandingCount, Is.EqualTo(0));
    }

    [Test]
    public void CorrelateReply_NoMatchingRequest_ReturnsNull()
    {
        var sut = new SmartProxy(_logger);
        var reply = CreateEnvelope("response");

        var correlation = sut.CorrelateReply(reply);

        Assert.That(correlation, Is.Null);
    }

    [Test]
    public void CorrelateReply_AlreadyCorrelated_ReturnsNullOnSecondCall()
    {
        var sut = new SmartProxy(_logger);
        var correlationId = Guid.NewGuid();
        var request = CreateEnvelope("req", replyTo: "reply-topic", correlationId: correlationId);
        var reply = CreateEnvelope("resp", correlationId: correlationId);

        sut.TrackRequest(request);
        sut.CorrelateReply(reply);

        var secondCorrelation = sut.CorrelateReply(reply);

        Assert.That(secondCorrelation, Is.Null);
    }

    [Test]
    public void TrackRequest_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = new SmartProxy(_logger);

        Assert.Throws<ArgumentNullException>(() =>
            sut.TrackRequest<string>(null!));
    }

    private static IntegrationEnvelope<string> CreateEnvelope(
        string payload,
        string? replyTo = null,
        Guid? correlationId = null) =>
        IntegrationEnvelope<string>.Create(
            payload,
            "TestService",
            "test.message",
            correlationId: correlationId) with
        {
            ReplyTo = replyTo,
        };
}

/// <summary>
/// Tests for the Control Bus system management pattern.
/// </summary>
[TestFixture]
public sealed class ControlBusTests
{
    private IMessageBrokerProducer _producer = null!;
    private IMessageBrokerConsumer _consumer = null!;
    private ILogger<ControlBusPublisher> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _logger = Substitute.For<ILogger<ControlBusPublisher>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    [Test]
    public async Task PublishCommandAsync_PublishesControlMessage()
    {
        var options = Options.Create(new ControlBusOptions());
        var sut = new ControlBusPublisher(_producer, _consumer, options, _logger);

        var result = await sut.PublishCommandAsync("reload-config", "config.reload");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ControlTopic, Is.EqualTo("eip.control-bus"));

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.MessageType == "config.reload" &&
                e.Intent == MessageIntent.Command),
            Arg.Is("eip.control-bus"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishCommandAsync_ProducerFails_ReturnsFailure()
    {
        _producer.PublishAsync(
                Arg.Any<IntegrationEnvelope<string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var options = Options.Create(new ControlBusOptions());
        var sut = new ControlBusPublisher(_producer, _consumer, options, _logger);

        var result = await sut.PublishCommandAsync("reload", "config.reload");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Connection lost"));
    }

    [Test]
    public void PublishCommandAsync_NullCommand_ThrowsArgumentNullException()
    {
        var options = Options.Create(new ControlBusOptions());
        var sut = new ControlBusPublisher(_producer, _consumer, options, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.PublishCommandAsync<string>(null!, "type"));
    }

    [Test]
    public void PublishCommandAsync_NullCommandType_ThrowsArgumentException()
    {
        var options = Options.Create(new ControlBusOptions());
        var sut = new ControlBusPublisher(_producer, _consumer, options, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.PublishCommandAsync("cmd", null!));
    }
}

/// <summary>
/// Tests for the Message Store system management pattern.
/// </summary>
[TestFixture]
public sealed class MessageStoreTests
{
    private IMessageRepository _repository = null!;
    private ILogger<MessageStore> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IMessageRepository>();
        _logger = Substitute.For<ILogger<MessageStore>>();
    }

    [Test]
    public async Task GetTrailAsync_ReturnsEntries()
    {
        var correlationId = Guid.NewGuid();
        var records = new List<MessageRecord>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "order.created",
                Source = "OrderService",
                PayloadJson = "{}",
                RecordedAt = DateTimeOffset.UtcNow,
                DeliveryStatus = DeliveryStatus.Delivered
            }
        };

        _repository.GetByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(records);

        var sut = new MessageStore(_repository, _logger);

        var trail = await sut.GetTrailAsync(correlationId);

        Assert.That(trail, Has.Count.EqualTo(1));
        Assert.That(trail[0].MessageType, Is.EqualTo("order.created"));
        Assert.That(trail[0].Status, Is.EqualTo(DeliveryStatus.Delivered));
    }

    [Test]
    public async Task GetByIdAsync_Found_ReturnsEntry()
    {
        var messageId = Guid.NewGuid();
        var record = new MessageRecord
        {
            MessageId = messageId,
            CorrelationId = Guid.NewGuid(),
            MessageType = "test.msg",
            Source = "Test",
            PayloadJson = "{}",
            RecordedAt = DateTimeOffset.UtcNow
        };

        _repository.GetByMessageIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(record);

        var sut = new MessageStore(_repository, _logger);

        var entry = await sut.GetByIdAsync(messageId);

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.MessageId, Is.EqualTo(messageId));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _repository.GetByMessageIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MessageRecord?)null);

        var sut = new MessageStore(_repository, _logger);

        var entry = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(entry, Is.Null);
    }

    [Test]
    public async Task GetFaultCountAsync_ReturnsFaultCount()
    {
        var correlationId = Guid.NewGuid();
        var faults = new List<FaultEnvelope>
        {
            new() { FaultId = Guid.NewGuid(), OriginalMessageId = Guid.NewGuid(), CorrelationId = correlationId, FaultedAt = DateTimeOffset.UtcNow, FaultReason = "fail", FaultedBy = "Test", OriginalMessageType = "test", RetryCount = 0 },
            new() { FaultId = Guid.NewGuid(), OriginalMessageId = Guid.NewGuid(), CorrelationId = correlationId, FaultedAt = DateTimeOffset.UtcNow, FaultReason = "fail2", FaultedBy = "Test", OriginalMessageType = "test", RetryCount = 1 },
        };

        _repository.GetFaultsByCorrelationIdAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(faults);

        var sut = new MessageStore(_repository, _logger);

        var count = await sut.GetFaultCountAsync(correlationId);

        Assert.That(count, Is.EqualTo(2));
    }
}
