using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests;

/// <summary>
/// Tests for consumer EIP patterns: PollingConsumer, EventDrivenConsumer,
/// SelectiveConsumer, and DurableSubscriber.
/// </summary>
[TestFixture]
public sealed class ConsumerPatternTests
{
    private IMessageBrokerConsumer _consumer = null!;

    [SetUp]
    public void SetUp()
    {
        _consumer = Substitute.For<IMessageBrokerConsumer>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _consumer.DisposeAsync();
    }

    // ── PollingConsumer ─────────────────────────────────────────────────────

    [Test]
    public async Task PollingConsumer_PollAsync_ReturnsMessages()
    {
        var envelope = CreateEnvelope("polled-msg");

        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var handler = callInfo.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                return handler(envelope);
            });

        var logger = Substitute.For<ILogger<PollingConsumer>>();
        var sut = new PollingConsumer(_consumer, logger);

        var result = await sut.PollAsync<string>("topic", "group", maxMessages: 5);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Payload, Is.EqualTo("polled-msg"));
    }

    [Test]
    public void PollingConsumer_ZeroMaxMessages_ThrowsArgumentOutOfRange()
    {
        var logger = Substitute.For<ILogger<PollingConsumer>>();
        var sut = new PollingConsumer(_consumer, logger);

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await sut.PollAsync<string>("topic", "group", maxMessages: 0));
    }

    [Test]
    public void PollingConsumer_NullConsumer_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<PollingConsumer>>();
        Assert.Throws<ArgumentNullException>(() => new PollingConsumer(null!, logger));
    }

    [Test]
    public void PollingConsumer_NullTopic_ThrowsArgumentException()
    {
        var logger = Substitute.For<ILogger<PollingConsumer>>();
        var sut = new PollingConsumer(_consumer, logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.PollAsync<string>(null!, "group"));
    }

    // ── EventDrivenConsumer ─────────────────────────────────────────────────

    [Test]
    public async Task EventDrivenConsumer_StartAsync_DelegatesToBrokerConsumer()
    {
        var logger = Substitute.For<ILogger<EventDrivenConsumer>>();
        var sut = new EventDrivenConsumer(_consumer, logger);
        var handlerCalled = false;

        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            });

        await sut.StartAsync<string>("topic", "group", _ => Task.CompletedTask);

        Assert.That(handlerCalled, Is.True);
        await _consumer.Received(1).SubscribeAsync(
            Arg.Is("topic"),
            Arg.Is("group"),
            Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void EventDrivenConsumer_NullHandler_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<EventDrivenConsumer>>();
        var sut = new EventDrivenConsumer(_consumer, logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.StartAsync<string>("topic", "group", null!));
    }

    [Test]
    public void EventDrivenConsumer_NullConsumer_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<EventDrivenConsumer>>();
        Assert.Throws<ArgumentNullException>(() => new EventDrivenConsumer(null!, logger));
    }

    // ── SelectiveConsumer ───────────────────────────────────────────────────

    [Test]
    public async Task SelectiveConsumer_MatchingPredicate_InvokesHandler()
    {
        var matchingEnvelope = CreateEnvelope("high-priority");
        matchingEnvelope = matchingEnvelope with { Priority = MessagePriority.High };

        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var handler = callInfo.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                await handler(matchingEnvelope);
            });

        var logger = Substitute.For<ILogger<SelectiveConsumer>>();
        var sut = new SelectiveConsumer(_consumer, logger);
        var handled = new List<IntegrationEnvelope<string>>();

        await sut.SubscribeAsync<string>(
            "topic",
            "group",
            env => env.Priority == MessagePriority.High,
            async env => handled.Add(env));

        Assert.That(handled, Has.Count.EqualTo(1));
        Assert.That(handled[0].Priority, Is.EqualTo(MessagePriority.High));
    }

    [Test]
    public async Task SelectiveConsumer_NonMatchingPredicate_SkipsMessage()
    {
        var lowPriorityEnvelope = CreateEnvelope("low-priority");

        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var handler = callInfo.ArgAt<Func<IntegrationEnvelope<string>, Task>>(2);
                await handler(lowPriorityEnvelope);
            });

        var logger = Substitute.For<ILogger<SelectiveConsumer>>();
        var sut = new SelectiveConsumer(_consumer, logger);
        var handled = new List<IntegrationEnvelope<string>>();

        await sut.SubscribeAsync<string>(
            "topic",
            "group",
            env => env.Priority == MessagePriority.High,
            async env => handled.Add(env));

        Assert.That(handled, Is.Empty);
    }

    [Test]
    public void SelectiveConsumer_NullPredicate_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<SelectiveConsumer>>();
        var sut = new SelectiveConsumer(_consumer, logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.SubscribeAsync<string>(
                "topic", "group", null!, _ => Task.CompletedTask));
    }

    // ── DurableSubscriber ───────────────────────────────────────────────────

    [Test]
    public async Task DurableSubscriber_SubscribeAsync_SetsIsConnected()
    {
        var logger = Substitute.For<ILogger<DurableSubscriber>>();
        var sut = new DurableSubscriber(_consumer, logger);

        Assert.That(sut.IsConnected, Is.False);

        var tcs = new TaskCompletionSource();
        var wasConnectedDuringSubscribe = false;

        _consumer.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<IntegrationEnvelope<string>, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                wasConnectedDuringSubscribe = sut.IsConnected;
                tcs.SetResult();
                await Task.CompletedTask;
            });

        await sut.SubscribeAsync<string>("topic", "durable-sub", _ => Task.CompletedTask);
        await tcs.Task;

        Assert.That(wasConnectedDuringSubscribe, Is.True);
        Assert.That(sut.IsConnected, Is.False); // After subscribe completes, disconnected
    }

    [Test]
    public async Task DurableSubscriber_DisposeAsync_SetsIsConnectedFalse()
    {
        var logger = Substitute.For<ILogger<DurableSubscriber>>();
        var sut = new DurableSubscriber(_consumer, logger);

        await sut.DisposeAsync();

        Assert.That(sut.IsConnected, Is.False);
    }

    [Test]
    public void DurableSubscriber_NullConsumer_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<DurableSubscriber>>();
        Assert.Throws<ArgumentNullException>(() => new DurableSubscriber(null!, logger));
    }

    [Test]
    public void DurableSubscriber_NullSubscriptionName_ThrowsArgumentException()
    {
        var logger = Substitute.For<ILogger<DurableSubscriber>>();
        var sut = new DurableSubscriber(_consumer, logger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.SubscribeAsync<string>("topic", null!, _ => Task.CompletedTask));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static IntegrationEnvelope<string> CreateEnvelope(string payload) =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "test.message");
}
