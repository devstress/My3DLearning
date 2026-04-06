// ============================================================================
// Tutorial 46 – Complete Integration / Demo Pipeline (Lab)
// ============================================================================
// This lab exercises the PipelineOrchestrator, PipelineOptions,
// IntegrationPipelineInput/Result, and ITemporalWorkflowDispatcher.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial46;

[TestFixture]
public sealed class Lab
{
    // ── PipelineOptions Properties ──────────────────────────────────────────

    [Test]
    public void PipelineOptions_PropertiesAssignable()
    {
        var opts = new PipelineOptions
        {
            AckSubject = "ack-topic",
            NackSubject = "nack-topic",
        };

        Assert.That(opts.AckSubject, Is.EqualTo("ack-topic"));
        Assert.That(opts.NackSubject, Is.EqualTo("nack-topic"));
    }

    // ── IntegrationPipelineInput Record Shape ───────────────────────────────

    [Test]
    public void IntegrationPipelineInput_RecordShape()
    {
        var input = new IntegrationPipelineInput(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
            "OrderService", "order.created", "1.0", 1, "{}", null, "ack", "nack");

        Assert.That(input.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(input.Source, Is.EqualTo("OrderService"));
        Assert.That(input.AckSubject, Is.EqualTo("ack"));
    }

    // ── IntegrationPipelineResult Record Shape ──────────────────────────────

    [Test]
    public void IntegrationPipelineResult_RecordShape()
    {
        var result = new IntegrationPipelineResult(Guid.NewGuid(), true);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.FailureReason, Is.Null);
    }

    // ── PipelineOrchestrator Dispatches To Workflow ──────────────────────────

    [Test]
    public async Task PipelineOrchestrator_ProcessAsync_DispatchesToWorkflow()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(Guid.NewGuid(), true));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"),
            "TestService", "test.event");

        await orchestrator.ProcessAsync(envelope);

        await dispatcher.Received(1).DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── IPipelineOrchestrator Interface Shape ────────────────────────────────

    [Test]
    public void IPipelineOrchestrator_InterfaceShape()
    {
        var type = typeof(IPipelineOrchestrator);

        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.GetMethod("ProcessAsync"), Is.Not.Null);
    }

    // ── ITemporalWorkflowDispatcher Interface Shape ─────────────────────────

    [Test]
    public void ITemporalWorkflowDispatcher_InterfaceShape()
    {
        var type = typeof(ITemporalWorkflowDispatcher);

        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.GetMethod("DispatchAsync"), Is.Not.Null);
    }

    // ── PipelineOrchestrator Uses Options ────────────────────────────────────

    [Test]
    public async Task PipelineOrchestrator_UsesAckNackFromOptions()
    {
        IntegrationPipelineInput? captured = null;
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => captured = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(Guid.NewGuid(), true));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "my-ack",
            NackSubject = "my-nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"),
            "Svc", "evt");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.AckSubject, Is.EqualTo("my-ack"));
        Assert.That(captured.NackSubject, Is.EqualTo("my-nack"));
    }
}
