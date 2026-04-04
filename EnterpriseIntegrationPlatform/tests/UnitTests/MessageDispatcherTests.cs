using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests;

/// <summary>
/// Tests for the Message Dispatcher and Service Activator EIP patterns.
/// </summary>
[TestFixture]
public sealed class MessageDispatcherTests
{
    private IOptions<MessageDispatcherOptions> _dispatcherOptions = null!;
    private ILogger<MessageDispatcher> _dispatcherLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _dispatcherOptions = Options.Create(new MessageDispatcherOptions());
        _dispatcherLogger = Substitute.For<ILogger<MessageDispatcher>>();
    }

    // ── Dispatch by type ────────────────────────────────────────────────────

    [Test]
    public async Task DispatchAsync_RegisteredType_InvokesHandlerAndSucceeds()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        var handled = false;

        sut.Register<string>("order.created", (env, ct) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        var envelope = CreateEnvelope("payload", "order.created");

        var result = await sut.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.MessageType, Is.EqualTo("order.created"));
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(handled, Is.True);
    }

    [Test]
    public async Task DispatchAsync_MultipleTypes_DispatchesToCorrectHandler()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        var orderHandled = false;
        var invoiceHandled = false;

        sut.Register<string>("order.created", (_, _) =>
        {
            orderHandled = true;
            return Task.CompletedTask;
        });

        sut.Register<string>("invoice.generated", (_, _) =>
        {
            invoiceHandled = true;
            return Task.CompletedTask;
        });

        var envelope = CreateEnvelope("invoice-data", "invoice.generated");

        await sut.DispatchAsync(envelope);

        Assert.That(orderHandled, Is.False);
        Assert.That(invoiceHandled, Is.True);
    }

    [Test]
    public async Task DispatchAsync_CaseInsensitiveLookup_MatchesHandler()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        var handled = false;

        sut.Register<string>("Order.Created", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        var envelope = CreateEnvelope("data", "order.created");

        var result = await sut.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(handled, Is.True);
    }

    // ── Unknown type handling ───────────────────────────────────────────────

    [Test]
    public async Task DispatchAsync_UnknownType_ReturnsFailed()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        var envelope = CreateEnvelope("data", "unknown.type");

        var result = await sut.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("No handler registered"));
    }

    [Test]
    public void DispatchAsync_UnknownType_ThrowOnUnknownType_ThrowsInvalidOperation()
    {
        var options = Options.Create(new MessageDispatcherOptions { ThrowOnUnknownType = true });
        var sut = new MessageDispatcher(options, _dispatcherLogger);

        var envelope = CreateEnvelope("data", "unknown.type");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.DispatchAsync(envelope));
    }

    // ── Handler failure ─────────────────────────────────────────────────────

    [Test]
    public async Task DispatchAsync_HandlerThrows_ReturnsFailedWithReason()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        sut.Register<string>("order.created", (_, _) =>
            throw new InvalidOperationException("Simulated handler failure"));

        var envelope = CreateEnvelope("data", "order.created");

        var result = await sut.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Simulated handler failure"));
    }

    // ── Register / Unregister ───────────────────────────────────────────────

    [Test]
    public async Task Register_ReplaceExistingHandler_OverwritesPrevious()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        var callCount = 0;

        sut.Register<string>("order.created", (_, _) =>
        {
            callCount = 1;
            return Task.CompletedTask;
        });
        sut.Register<string>("order.created", (_, _) =>
        {
            callCount = 2;
            return Task.CompletedTask;
        });

        Assert.That(sut.RegisteredTypes, Has.Count.EqualTo(1));

        var envelope = CreateEnvelope("data", "order.created");
        await sut.DispatchAsync(envelope);

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void Unregister_ExistingType_ReturnsTrue()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        sut.Register<string>("order.created", (_, _) => Task.CompletedTask);

        var removed = sut.Unregister("order.created");

        Assert.That(removed, Is.True);
        Assert.That(sut.RegisteredTypes, Is.Empty);
    }

    [Test]
    public void Unregister_NonExistentType_ReturnsFalse()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        Assert.That(sut.Unregister("nonexistent"), Is.False);
    }

    [Test]
    public void RegisteredTypes_ReturnsAllRegisteredTypes()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);
        sut.Register<string>("type-a", (_, _) => Task.CompletedTask);
        sut.Register<string>("type-b", (_, _) => Task.CompletedTask);
        sut.Register<string>("type-c", (_, _) => Task.CompletedTask);

        Assert.That(sut.RegisteredTypes, Has.Count.EqualTo(3));
        Assert.That(sut.RegisteredTypes, Does.Contain("type-a"));
        Assert.That(sut.RegisteredTypes, Does.Contain("type-b"));
        Assert.That(sut.RegisteredTypes, Does.Contain("type-c"));
    }

    // ── Null argument validation ────────────────────────────────────────────

    [Test]
    public void DispatchAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.DispatchAsync<string>(null!));
    }

    [Test]
    public void Register_NullMessageType_ThrowsArgumentException()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        Assert.Throws<ArgumentNullException>(() =>
            sut.Register<string>(null!, (_, _) => Task.CompletedTask));
    }

    [Test]
    public void Register_NullHandler_ThrowsArgumentNullException()
    {
        var sut = new MessageDispatcher(_dispatcherOptions, _dispatcherLogger);

        Assert.Throws<ArgumentNullException>(() =>
            sut.Register<string>("type", null!));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static IntegrationEnvelope<string> CreateEnvelope(string payload, string messageType = "test.message") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", messageType);
}

/// <summary>
/// Tests for the Service Activator EIP pattern.
/// </summary>
[TestFixture]
public sealed class ServiceActivatorTests
{
    private IMessageBrokerProducer _producer = null!;
    private IOptions<ServiceActivatorOptions> _activatorOptions = null!;
    private ILogger<ServiceActivator> _activatorLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _activatorOptions = Options.Create(new ServiceActivatorOptions());
        _activatorLogger = Substitute.For<ILogger<ServiceActivator>>();
    }

    // ── Invoke with reply ───────────────────────────────────────────────────

    [Test]
    public async Task InvokeAsync_WithReplyTo_PublishesReply()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        var envelope = CreateEnvelope("request-data", replyTo: "reply-topic");

        var result = await sut.InvokeAsync<string, string>(
            envelope,
            (env, ct) => Task.FromResult<string?>("response-data"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        Assert.That(result.ReplyTopic, Is.EqualTo("reply-topic"));

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.Payload == "response-data" &&
                e.CorrelationId == envelope.CorrelationId &&
                e.CausationId == envelope.MessageId),
            Arg.Is("reply-topic"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvokeAsync_WithReplyTo_SetsCorrectSourceAndMessageType()
    {
        var options = Options.Create(new ServiceActivatorOptions
        {
            ReplySource = "MyService",
            ReplyMessageType = "my.reply"
        });
        var sut = new ServiceActivator(_producer, options, _activatorLogger);

        var envelope = CreateEnvelope("data", replyTo: "reply-topic");

        await sut.InvokeAsync<string, string>(
            envelope,
            (_, _) => Task.FromResult<string?>("response"));

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e =>
                e.Source == "MyService" &&
                e.MessageType == "my.reply"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Invoke without reply ────────────────────────────────────────────────

    [Test]
    public async Task InvokeAsync_WithoutReplyTo_DoesNotPublish()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        var envelope = CreateEnvelope("data"); // no ReplyTo

        var result = await sut.InvokeAsync<string, string>(
            envelope,
            (_, _) => Task.FromResult<string?>("response"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);
        Assert.That(result.ReplyTopic, Is.Null);

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvokeAsync_NullResponse_DoesNotPublish()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        var envelope = CreateEnvelope("data", replyTo: "reply-topic");

        var result = await sut.InvokeAsync<string, string>(
            envelope,
            (_, _) => Task.FromResult<string?>(null));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Fire-and-forget invoke ──────────────────────────────────────────────

    [Test]
    public async Task InvokeAsync_FireAndForget_Succeeds()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);
        var invoked = false;

        var envelope = CreateEnvelope("data");

        var result = await sut.InvokeAsync(
            envelope,
            (_, _) =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);
        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task InvokeAsync_FireAndForget_OperationThrows_ReturnsFailed()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        var envelope = CreateEnvelope("data");

        var result = await sut.InvokeAsync(
            envelope,
            (Func<IntegrationEnvelope<string>, CancellationToken, Task>)((_, _) =>
                throw new InvalidOperationException("Service down")));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Service down"));
    }

    // ── Service operation failure ───────────────────────────────────────────

    [Test]
    public async Task InvokeAsync_OperationThrows_ReturnsFailed()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        var envelope = CreateEnvelope("data", replyTo: "reply-topic");

        var result = await sut.InvokeAsync<string, string>(
            envelope,
            (Func<IntegrationEnvelope<string>, CancellationToken, Task<string?>>)((_, _) =>
                throw new TimeoutException("External API timeout")));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ReplySent, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("External API timeout"));

        await _producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Null argument validation ────────────────────────────────────────────

    [Test]
    public void InvokeAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.InvokeAsync<string, string>(
                null!,
                (_, _) => Task.FromResult<string?>("reply")));
    }

    [Test]
    public void InvokeAsync_NullServiceOperation_ThrowsArgumentNullException()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.InvokeAsync<string, string>(
                CreateEnvelope("data"),
                null!));
    }

    [Test]
    public void InvokeAsync_FireAndForget_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.InvokeAsync<string>(null!, (_, _) => Task.CompletedTask));
    }

    [Test]
    public void InvokeAsync_FireAndForget_NullOperation_ThrowsArgumentNullException()
    {
        var sut = new ServiceActivator(_producer, _activatorOptions, _activatorLogger);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.InvokeAsync(CreateEnvelope("data"), (Func<IntegrationEnvelope<string>, CancellationToken, Task>)null!));
    }

    [Test]
    public void Constructor_NullProducer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceActivator(null!, _activatorOptions, _activatorLogger));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static IntegrationEnvelope<string> CreateEnvelope(
        string payload,
        string? replyTo = null,
        string messageType = "test.command") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", messageType) with
        {
            ReplyTo = replyTo,
            Intent = MessageIntent.Command,
        };
}
