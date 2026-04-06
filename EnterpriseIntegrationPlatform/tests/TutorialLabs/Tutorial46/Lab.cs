// ============================================================================
// Tutorial 46 – Complete Integration / Demo Pipeline (Lab)
// ============================================================================
// EIP Pattern: Message Dispatcher + Service Activator + Pipeline Orchestration.
// E2E: Wire MessageDispatcher and ServiceActivator with MockEndpoint to verify
// end-to-end dispatch, handler invocation, and reply publishing.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial46;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("pipeline-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

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
    public async Task Dispatcher_DispatchAndPublish_MockEndpointReceives()
    {
        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        dispatcher.Register<string>("order.created", async (env, _) =>
        {
            var outEnvelope = IntegrationEnvelope<string>.Create(
                $"processed:{env.Payload}", "pipeline", "order.processed");
            await _output.PublishAsync(outEnvelope, "processed-orders");
        });

        var envelope = IntegrationEnvelope<string>.Create("ORD-001", "svc", "order.created");
        await dispatcher.DispatchAsync(envelope);

        _output.AssertReceivedOnTopic("processed-orders", 1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("ORD-001"));
    }

    [Test]
    public async Task ServiceActivator_InvokeWithReply_PublishesToReplyTopic()
    {
        var activator = new ServiceActivator(
            _output, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("request", "svc", "order.query") with
        {
            ReplyTo = "reply-topic",
        };

        var result = await activator.InvokeAsync<string, string>(
            envelope, (env, _) => Task.FromResult<string?>($"response:{env.Payload}"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        _output.AssertReceivedOnTopic("reply-topic", 1);
    }

    [Test]
    public async Task ServiceActivator_NoReplyTo_NoReplyPublished()
    {
        var activator = new ServiceActivator(
            _output, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("request", "svc", "order.query");

        var result = await activator.InvokeAsync<string, string>(
            envelope, (_, _) => Task.FromResult<string?>("response"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);
        _output.AssertNoneReceived();
    }

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
