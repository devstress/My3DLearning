// ============================================================================
// Tutorial 14 – Process Manager (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Verify priority enum is cast to int in pipeline input
//   🟡 Intermediate — Confirm workflow ID is deterministic and idempotent
//   🔴 Advanced     — Ensure causation ID, timestamp, and schema version survive mapping
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial14;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Priority enum cast to integer in pipeline input ───

    [Test]
    public async Task Starter_PriorityMapping_CastsEnumToInt()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"item\":\"widget\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "order.created") with
        {
            Priority = MessagePriority.High,
        };

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.Priority, Is.EqualTo((int)MessagePriority.High));
    }

    // ── 🟡 INTERMEDIATE — Idempotent workflow ID from MessageId ─────────

    [Test]
    public async Task Intermediate_IdempotentWorkflowId_DeterministicFromMessageId()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"data\":1}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var expectedId = $"integration-{envelope.MessageId}";
        Assert.That(dispatcher.LastWorkflowId, Is.EqualTo(expectedId));
    }

    // ── 🔴 ADVANCED — CausationId, Timestamp, and SchemaVersion preserved ─

    [Test]
    public async Task Advanced_CausationIdAndTimestamp_PreservedInInput()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);

        var causationId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
        var json = JsonSerializer.Deserialize<JsonElement>("{\"v\":1}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type") with
        {
            CausationId = causationId,
            Timestamp = timestamp,
        };

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.CausationId, Is.EqualTo(causationId));
        Assert.That(captured.Timestamp, Is.EqualTo(timestamp));
        Assert.That(captured.SchemaVersion, Is.EqualTo(envelope.SchemaVersion));
    }

    private static PipelineOrchestrator CreateOrchestrator(
        MockTemporalWorkflowDispatcher dispatcher)
    {
        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "integration.ack",
            NackSubject = "integration.nack",
        });
        return new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);
    }
}
