// ============================================================================
// Tutorial 14 – Process Manager (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Process Manager pattern
//          converts an IntegrationEnvelope into an IntegrationPipelineInput
//          and dispatches it to a Temporal workflow.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. ProcessAsync_DispatchesCorrectWorkflowId          — workflow ID derived from MessageId
//   2. ProcessAsync_MapsEnvelopeFieldsToInput             — core envelope fields mapped to input
//   3. ProcessAsync_SerializesPayloadAsJson               — payload serialized as JSON string
//   4. ProcessAsync_WithMetadata_SerializesMetadataJson   — metadata dictionary serialized to JSON
//   5. ProcessAsync_EmptyMetadata_SetsMetadataJsonNull    — empty metadata maps to null JSON
//   6. ProcessAsync_SetsAckAndNackSubjectsFromOptions     — Ack/Nack subjects from PipelineOptions
//
// INFRASTRUCTURE: MockTemporalWorkflowDispatcher (no real Temporal server), NUnit
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
public sealed class Lab
{
    // ── 1. Workflow Dispatching ────────────────────────────────────────

    [Test]
    public async Task ProcessAsync_DispatchesCorrectWorkflowId()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var envelope = CreateEnvelope("order-data", "OrderService", "order.created");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        Assert.That(dispatcher.LastWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    [Test]
    public async Task ProcessAsync_MapsEnvelopeFieldsToInput()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var envelope = CreateEnvelope("payload-data", "TestSource", "test.type");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = dispatcher.LastInput;
        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(capturedInput.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(capturedInput.Source, Is.EqualTo("TestSource"));
        Assert.That(capturedInput.MessageType, Is.EqualTo("test.type"));
    }

    // ── 2. Envelope-to-Input Mapping ──────────────────────────────────

    [Test]
    public async Task ProcessAsync_SerializesPayloadAsJson()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\":\"value\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = dispatcher.LastInput;
        Assert.That(capturedInput!.PayloadJson, Does.Contain("key"));
        Assert.That(capturedInput.PayloadJson, Does.Contain("value"));
    }

    [Test]
    public async Task ProcessAsync_WithMetadata_SerializesMetadataJson()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var envelope = CreateEnvelope("data", "Svc", "test.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-east",
                ["tenant"] = "acme",
            },
        };

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = dispatcher.LastInput;
        Assert.That(capturedInput!.MetadataJson, Is.Not.Null);
        Assert.That(capturedInput.MetadataJson, Does.Contain("region"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("us-east"));
    }

    // ── 3. Pipeline Options ───────────────────────────────────────────

    [Test]
    public async Task ProcessAsync_EmptyMetadata_SetsMetadataJsonNull()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = dispatcher.LastInput;
        Assert.That(capturedInput!.MetadataJson, Is.Null);
    }

    [Test]
    public async Task ProcessAsync_SetsAckAndNackSubjectsFromOptions()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher();
        var orchestrator = CreateOrchestrator(dispatcher);
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = dispatcher.LastInput;
        Assert.That(capturedInput!.AckSubject, Is.EqualTo("integration.ack"));
        Assert.That(capturedInput.NackSubject, Is.EqualTo("integration.nack"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

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

    private static IntegrationEnvelope<JsonElement> CreateEnvelope(
        string payload, string source, string messageType)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(
            $"{{\"data\":\"{payload}\"}}");
        return IntegrationEnvelope<JsonElement>.Create(json, source, messageType);
    }
}
