// ============================================================================
// Tutorial 46 – Complete Integration / Demo Pipeline (Lab)
// ============================================================================
// EIP Pattern: Message Dispatcher + Service Activator + Pipeline Orchestration.
// E2E: Wire MessageDispatcher and ServiceActivator with NatsBrokerEndpoint
//      (real NATS JetStream via Aspire) to verify end-to-end dispatch,
//      handler invocation, and reply publishing.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial46;

[TestFixture]
public sealed class Lab
{

    // ── 1. Message Dispatcher ────────────────────────────────────────

    [Test]
    public async Task Dispatcher_RegisterAndDispatch_HandlerInvoked()
    {
        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        string? captured = null;
        dispatcher.Register<string>("order.created",
            (env, _) => { captured = env.Payload; return Task.CompletedTask; });

        var envelope = IntegrationEnvelope<string>.Create("order-data", "svc", "order.created");
        var result = await dispatcher.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(captured, Is.EqualTo("order-data"));
    }

    [Test]
    public async Task Dispatcher_UnknownType_ReturnsNotFound()
    {
        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "svc", "unknown.type");
        var result = await dispatcher.DispatchAsync(envelope);

        Assert.That(result.HandlerFound, Is.False);
        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task Dispatcher_DispatchAndPublish_NatsBrokerEndpointReceives()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t46-dispatch-publish");
        var topic = AspireFixture.UniqueTopic("t46-processed-orders");

        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        dispatcher.Register<string>("order.created", async (env, _) =>
        {
            var outEnvelope = IntegrationEnvelope<string>.Create(
                $"processed:{env.Payload}", "pipeline", "order.processed");
            await nats.PublishAsync(outEnvelope, topic, default);
        });

        var envelope = IntegrationEnvelope<string>.Create("ORD-001", "svc", "order.created");
        await dispatcher.DispatchAsync(envelope);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("ORD-001"));
    }


    // ── 2. Service Activator ─────────────────────────────────────────

    [Test]
    public async Task ServiceActivator_InvokeWithReply_PublishesToReplyTopic()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t46-reply");
        var replyTopic = AspireFixture.UniqueTopic("t46-reply");

        var activator = new ServiceActivator(
            nats, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("request", "svc", "order.query") with
        {
            ReplyTo = replyTopic,
        };

        var result = await activator.InvokeAsync<string, string>(
            envelope, (env, _) => Task.FromResult<string?>($"response:{env.Payload}"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        nats.AssertReceivedOnTopic(replyTopic, 1);
    }

    [Test]
    public async Task ServiceActivator_NoReplyTo_NoReplyPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t46-noreply");

        var activator = new ServiceActivator(
            nats, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("request", "svc", "order.query");

        var result = await activator.InvokeAsync<string, string>(
            envelope, (_, _) => Task.FromResult<string?>("response"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);
        nats.AssertNoneReceived();
    }


    // ── 3. Pipeline Orchestration ────────────────────────────────────

    [Test]
    public async Task PipelineOrchestrator_ProcessAsync_DispatchesToWorkflow()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions { AckSubject = "ack", NackSubject = "nack" }),
            NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"), "TestService", "test.event");
        await orchestrator.ProcessAsync(envelope);

        dispatcher.AssertDispatchCount(1);
    }

    [Test]
    public async Task PipelineOrchestrator_MapsAckNackFromOptions()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var orchestrator = new PipelineOrchestrator(
            dispatcher, Options.Create(new PipelineOptions { AckSubject = "my-ack", NackSubject = "my-nack" }),
            NullLogger<PipelineOrchestrator>.Instance);

        await orchestrator.ProcessAsync(IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"), "Svc", "evt"));

        Assert.That(dispatcher.LastInput, Is.Not.Null);
        Assert.That(dispatcher.LastInput!.AckSubject, Is.EqualTo("my-ack"));
        Assert.That(dispatcher.LastInput.NackSubject, Is.EqualTo("my-nack"));
    }
}
