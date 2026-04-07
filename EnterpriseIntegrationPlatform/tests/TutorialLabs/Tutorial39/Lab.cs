// ============================================================================
// Tutorial 39 – Message Lifecycle / System Management (Lab)
// ============================================================================
// EIP Pattern: Smart Proxy + Control Bus.
// E2E: Wire real SmartProxy with MockEndpoint for request-reply tracking,
// and real ControlBusPublisher with MockEndpoint as both producer and consumer.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.SystemManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial39;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _bus = null!;

    [SetUp]
    public void SetUp() => _bus = new MockEndpoint("control-bus");

    [TearDown]
    public async Task TearDown() => await _bus.DisposeAsync();


    // ── 1. Smart Proxy – Request Tracking ────────────────────────────

    [Test]
    public void SmartProxy_TrackRequest_IncrementsOutstanding()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);
        var request = CreateEnvelopeWithReplyTo("req", "Svc", "cmd.query", "reply-queue");

        var tracked = proxy.TrackRequest(request);

        Assert.That(tracked, Is.True);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(1));
    }

    [Test]
    public void SmartProxy_CorrelateReply_ReturnsCorrelation()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);
        var request = CreateEnvelopeWithReplyTo("req", "Svc", "cmd.query", "reply-queue");
        proxy.TrackRequest(request);

        var reply = IntegrationEnvelope<string>.Create(
            "response", "ReplySvc", "cmd.response",
            correlationId: request.CorrelationId);

        var correlation = proxy.CorrelateReply(reply);

        Assert.That(correlation, Is.Not.Null);
        Assert.That(correlation!.CorrelationId, Is.EqualTo(request.CorrelationId));
        Assert.That(correlation.OriginalReplyTo, Is.EqualTo("reply-queue"));
        Assert.That(correlation.RequestMessageId, Is.EqualTo(request.MessageId));
        Assert.That(proxy.OutstandingCount, Is.EqualTo(0));
    }


    // ── 2. Control Bus – Publish & Subscribe ─────────────────────────

    [Test]
    public void SmartProxy_CorrelateReply_ReturnsNull_ForUnknown()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);
        var unknown = IntegrationEnvelope<string>.Create("data", "Svc", "unknown.reply");

        Assert.That(proxy.CorrelateReply(unknown), Is.Null);
    }

    [Test]
    public void SmartProxy_NoReplyTo_ReturnsFalse()
    {
        var proxy = new SmartProxy(NullLogger<SmartProxy>.Instance);
        var noReply = IntegrationEnvelope<string>.Create("data", "Svc", "cmd.query");

        var tracked = proxy.TrackRequest(noReply);

        Assert.That(tracked, Is.False);
        Assert.That(proxy.OutstandingCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ControlBus_PublishCommand_MockEndpoint_CapturesMessage()
    {
        var publisher = new ControlBusPublisher(
            _bus, _bus,
            Options.Create(new ControlBusOptions
            {
                ControlTopic = "eip.control",
                Source = "TestBus",
            }),
            NullLogger<ControlBusPublisher>.Instance);

        var result = await publisher.PublishCommandAsync(
            new { Action = "restart" }, "system.restart");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ControlTopic, Is.EqualTo("eip.control"));
        _bus.AssertReceivedOnTopic("eip.control", 1);
    }


    // ── 3. End-to-End Roundtrip ──────────────────────────────────────

    [Test]
    public async Task ControlBus_Subscribe_MockEndpoint_DeliversCommand()
    {
        IntegrationEnvelope<string>? received = null;
        var publisher = new ControlBusPublisher(
            _bus, _bus,
            Options.Create(new ControlBusOptions
            {
                ControlTopic = "eip.control",
                ConsumerGroup = "ctrl-group",
                Source = "TestBus",
            }),
            NullLogger<ControlBusPublisher>.Instance);

        await publisher.SubscribeAsync<string>("system.restart",
            env => { received = env; return Task.CompletedTask; });

        // Feed a matching control command through MockEndpoint
        var command = IntegrationEnvelope<string>.Create(
            "restart-payload", "ControlBus", "system.restart") with
        {
            Intent = MessageIntent.Command,
        };
        await _bus.SendAsync(command);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.MessageType, Is.EqualTo("system.restart"));
    }

    [Test]
    public async Task ControlBus_PublishAndSubscribe_E2E_Roundtrip()
    {
        await using var output = new MockEndpoint("ctrl-output");
        IntegrationEnvelope<string>? captured = null;

        var publisher = new ControlBusPublisher(
            output, _bus,
            Options.Create(new ControlBusOptions
            {
                ControlTopic = "eip.control",
                ConsumerGroup = "ctrl-group",
                Source = "TestBus",
            }),
            NullLogger<ControlBusPublisher>.Instance);

        // Publish a command — lands on output MockEndpoint
        var result = await publisher.PublishCommandAsync("scale-up", "system.scale");

        Assert.That(result.Succeeded, Is.True);
        output.AssertReceivedOnTopic("eip.control", 1);

        // Subscribe for commands — uses _bus (consumer) MockEndpoint
        await publisher.SubscribeAsync<string>("system.scale",
            env => { captured = env; return Task.CompletedTask; });

        // Feed the command to the consumer side
        var cmd = IntegrationEnvelope<string>.Create(
            "scale-up", "ControlBus", "system.scale") with
        {
            Intent = MessageIntent.Command,
        };
        await _bus.SendAsync(cmd);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("scale-up"));
    }

    private static IntegrationEnvelope<string> CreateEnvelopeWithReplyTo(
        string payload, string source, string messageType, string replyTo) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Payload = payload,
            ReplyTo = replyTo,
        };
}
