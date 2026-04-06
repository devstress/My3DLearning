// ============================================================================
// Tutorial 14 – Process Manager (Exam)
// ============================================================================
// Coding challenges: verify metadata serialisation, test envelope-to-input
// priority mapping, and validate idempotent workflow ID generation.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial14;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Metadata Serialisation ──────────────────────────────────

    [Test]
    public async Task Challenge1_MetadataSerialisation_NullWhenEmpty()
    {
        // When the envelope has no metadata entries, MetadataJson in the
        // pipeline input should be null (not an empty JSON object).
        IntegrationPipelineInput? capturedInput = null;

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "event.type");
        // Ensure metadata is empty (default).

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MetadataJson, Is.Null);
    }

    [Test]
    public async Task Challenge1_MetadataSerialisation_PopulatedWhenPresent()
    {
        IntegrationPipelineInput? capturedInput = null;

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-east",
                ["tenant"] = "acme",
            },
        };

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MetadataJson, Is.Not.Null);
        Assert.That(capturedInput.MetadataJson, Does.Contain("us-east"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("acme"));
    }

    // ── Challenge 2: Priority Mapping — Enum to Int ─────────────────────────

    [Test]
    public async Task Challenge2_PriorityMapping_EnumCastsToInt()
    {
        // The PipelineOrchestrator maps MessagePriority enum to int.
        // Verify that High (2) and Critical (3) map correctly.
        IntegrationPipelineInput? capturedInput = null;

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");

        // Test Critical priority mapping.
        var criticalEnvelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "alert.critical") with
        {
            Priority = MessagePriority.Critical,
        };

        await orchestrator.ProcessAsync(criticalEnvelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.Priority, Is.EqualTo((int)MessagePriority.Critical));
    }

    // ── Challenge 3: Idempotent Workflow IDs ────────────────────────────────

    [Test]
    public async Task Challenge3_IdempotentWorkflowId_SameMessageProducesSameId()
    {
        // Processing the same envelope twice should produce the same workflow ID,
        // enabling Temporal's idempotency guarantees.
        var capturedIds = new List<string>();

        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedIds.Add(ci.ArgAt<string>(1));
                var input = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(input.MessageId, IsSuccess: true);
            });

        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "ack",
            NackSubject = "nack",
        });

        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Service", "event.type");

        // Process the same envelope twice.
        await orchestrator.ProcessAsync(envelope);
        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedIds, Has.Count.EqualTo(2));
        Assert.That(capturedIds[0], Is.EqualTo(capturedIds[1]));
        Assert.That(capturedIds[0], Is.EqualTo($"integration-{envelope.MessageId}"));
    }
}
