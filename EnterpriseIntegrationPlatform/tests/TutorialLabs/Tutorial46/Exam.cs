// ============================================================================
// Tutorial 46 – Complete Integration / Demo Pipeline (Exam)
// ============================================================================
// Coding challenges: full pipeline flow, error handling, and input mapping.
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
public sealed class Exam
{
    // ── Challenge 1: Full Pipeline Flow ──────────────────────────────────────

    [Test]
    public async Task Challenge1_FullPipelineFlow_DispatchesAndReturns()
    {
        var msgId = Guid.NewGuid();
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(msgId, true));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack-subject",
            NackSubject = "nack-subject",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var payload = JsonSerializer.Deserialize<JsonElement>(
            "{\"orderId\": \"ORD-001\", \"amount\": 99.99}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            payload, "OrderService", "order.created");

        await orchestrator.ProcessAsync(envelope);

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<IntegrationPipelineInput>(i =>
                i.Source == "OrderService" &&
                i.MessageType == "order.created"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Pipeline Input Mapping ─────────────────────────────────

    [Test]
    public async Task Challenge2_PipelineInputMapping_CapturesEnvelopeFields()
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
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{\"key\": \"value\"}"),
            "TestSource", "test.type");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Source, Is.EqualTo("TestSource"));
        Assert.That(captured.MessageType, Is.EqualTo("test.type"));
        Assert.That(captured.PayloadJson, Does.Contain("key"));
    }

    // ── Challenge 3: Dispatcher Failure Scenario ────────────────────────────

    [Test]
    public void Challenge3_DispatcherFailure_OrchestratorHandlesGracefully()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(Guid.NewGuid(), false, "Temporal unavailable"));

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"),
            "Svc", "evt");

        // Should not throw even on failure result
        Assert.DoesNotThrowAsync(() => orchestrator.ProcessAsync(envelope));
    }
}
