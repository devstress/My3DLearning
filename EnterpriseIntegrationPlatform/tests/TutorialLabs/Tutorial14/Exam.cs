// ============================================================================
// Tutorial 14 – Process Manager (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Process Manager pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Verify priority enum is cast to int in pipeline input
//   🟡 Intermediate — Confirm workflow ID is deterministic and idempotent
//   🔴 Advanced     — Ensure causation ID, timestamp, and schema version survive mapping
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockTemporalWorkflowDispatcher, PipelineOrchestrator, NUnit
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
public sealed class Exam
{
    // ── 🟢 STARTER — Priority enum cast to integer in pipeline input ───
    //
    // SCENARIO: An envelope with MessagePriority.High is dispatched through
    //           the PipelineOrchestrator. The captured input must contain
    //           the integer equivalent of the enum value.
    //
    // WHAT YOU PROVE: The orchestrator correctly casts the MessagePriority
    //                 enum to its underlying int when building the input.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: A message is dispatched to Temporal. The workflow ID must
    //           be deterministically derived as "integration-{MessageId}"
    //           so that retries do not create duplicate workflows.
    //
    // WHAT YOU PROVE: The orchestrator produces a stable, idempotent
    //                 workflow ID from the envelope's MessageId.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: An envelope is created with an explicit CausationId and a
    //           backdated Timestamp. After dispatch, the captured pipeline
    //           input must preserve all three traceability fields exactly.
    //
    // WHAT YOU PROVE: The orchestrator faithfully maps CausationId,
    //                 Timestamp, and SchemaVersion without alteration.
    // ─────────────────────────────────────────────────────────────────────

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
