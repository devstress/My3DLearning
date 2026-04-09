using EnterpriseIntegrationPlatform.Admin.Api.Services;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Replay;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ControlBusPublisherTests
{
    private IMessageBrokerProducer _producer = null!;
    private IMessageBrokerConsumer _consumer = null!;
    private ControlBusOptions _options = null!;
    private ControlBusPublisher _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _consumer = Substitute.For<IMessageBrokerConsumer>();
        _options = new ControlBusOptions
        {
            ControlTopic = "test.control",
            ConsumerGroup = "test-consumers",
            Source = "UnitTest",
        };
        _sut = new ControlBusPublisher(
            _producer,
            _consumer,
            Options.Create(_options),
            NullLogger<ControlBusPublisher>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    // ── PublishCommandAsync tests ────────────────────────────────────────

    [Test]
    public async Task PublishCommandAsync_Success_ReturnsSucceededTrue()
    {
        var result = await _sut.PublishCommandAsync(
            new { Action = "restart" },
            "RestartService");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ControlTopic, Is.EqualTo("test.control"));
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public async Task PublishCommandAsync_Success_PublishesToControlTopic()
    {
        await _sut.PublishCommandAsync(
            new { Action = "restart" },
            "RestartService");

        await _producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<object>>(),
            Arg.Is("test.control"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishCommandAsync_Success_SetsCommandIntent()
    {
        // Capture the envelope via When..Do pattern on the generic method
        MessageIntent? capturedIntent = null;
        string? capturedMessageType = null;
        string? capturedSource = null;

        _producer.When(x => x.PublishAsync(
                Arg.Any<IntegrationEnvelope<object>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var env = call.Arg<IntegrationEnvelope<object>>();
                capturedIntent = env.Intent;
                capturedMessageType = env.MessageType;
                capturedSource = env.Source;
            });

        await _sut.PublishCommandAsync<object>(
            new { Action = "restart" },
            "RestartService");

        Assert.That(capturedIntent, Is.EqualTo(MessageIntent.Command));
        Assert.That(capturedMessageType, Is.EqualTo("RestartService"));
        Assert.That(capturedSource, Is.EqualTo("UnitTest"));
    }

    [Test]
    public async Task PublishCommandAsync_ProducerThrows_ReturnsSucceededFalse()
    {
        _producer.PublishAsync(
                Arg.Any<IntegrationEnvelope<object>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        var result = await _sut.PublishCommandAsync(
            new { Action = "restart" },
            "RestartService");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("Broker unavailable"));
    }

    [Test]
    public void PublishCommandAsync_NullCommand_ThrowsArgumentNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _sut.PublishCommandAsync<object>(null!, "Test"));
    }

    [Test]
    public void PublishCommandAsync_EmptyCommandType_ThrowsArgument()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.PublishCommandAsync(new { }, ""));
    }

    // ── SubscribeAsync tests ────────────────────────────────────────────

    [Test]
    public async Task SubscribeAsync_RegistersConsumerOnControlTopic()
    {
        await _sut.SubscribeAsync<object>(
            "RestartService",
            _ => Task.CompletedTask);

        await _consumer.Received(1).SubscribeAsync(
            Arg.Is("test.control"),
            Arg.Is("test-consumers"),
            Arg.Any<Func<IntegrationEnvelope<object>, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void SubscribeAsync_NullHandler_ThrowsArgumentNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _sut.SubscribeAsync<object>("Test", null!));
    }

    [Test]
    public void SubscribeAsync_EmptyCommandType_ThrowsArgument()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.SubscribeAsync<object>("", _ => Task.CompletedTask));
    }

    // ── Constructor validation tests ────────────────────────────────────

    [Test]
    public void Constructor_NullProducer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ControlBusPublisher(
                null!,
                _consumer,
                Options.Create(_options),
                NullLogger<ControlBusPublisher>.Instance));
    }

    [Test]
    public void Constructor_NullConsumer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ControlBusPublisher(
                _producer,
                null!,
                Options.Create(_options),
                NullLogger<ControlBusPublisher>.Instance));
    }
}

[TestFixture]
public class DlqManagementServiceTests
{
    private IMessageReplayer _replayer = null!;
    private DlqManagementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _replayer = Substitute.For<IMessageReplayer>();
        _sut = new DlqManagementService(
            _replayer,
            NullLogger<DlqManagementService>.Instance);
    }

    [Test]
    public async Task ResubmitAsync_DelegatesToReplayer()
    {
        var filter = new ReplayFilter { MessageType = "OrderCreated" };
        var now = DateTimeOffset.UtcNow;
        _replayer.ReplayAsync(Arg.Any<ReplayFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ReplayResult
            {
                ReplayedCount = 5,
                FailedCount = 1,
                SkippedCount = 0,
                StartedAt = now,
                CompletedAt = now.AddSeconds(2),
            });

        var result = await _sut.ResubmitAsync(filter);

        Assert.That(result.ReplayedCount, Is.EqualTo(5));
        Assert.That(result.FailedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ResubmitAsync_PassesFilterToReplayer()
    {
        var correlationId = Guid.NewGuid();
        var filter = new ReplayFilter
        {
            CorrelationId = correlationId,
            MessageType = "OrderCreated",
        };
        var now = DateTimeOffset.UtcNow;
        _replayer.ReplayAsync(Arg.Any<ReplayFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ReplayResult
            {
                ReplayedCount = 0,
                FailedCount = 0,
                SkippedCount = 0,
                StartedAt = now,
                CompletedAt = now,
            });

        await _sut.ResubmitAsync(filter);

        await _replayer.Received(1).ReplayAsync(
            Arg.Is<ReplayFilter>(f =>
                f.CorrelationId == correlationId &&
                f.MessageType == "OrderCreated"),
            Arg.Any<CancellationToken>());
    }
}
