// ============================================================================
// Tutorial 14 – Process Manager (Lab)
// ============================================================================
// EIP Pattern: Process Manager
// E2E: PipelineOrchestrator converts IntegrationEnvelope to pipeline input
// and dispatches to Temporal. Uses MockTemporalWorkflowDispatcher since
// Temporal requires a real server.
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
    private MockTemporalWorkflowDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp() => _dispatcher = new MockTemporalWorkflowDispatcher();

    // ── 1. Workflow Dispatching ────────────────────────────────────────

    [Test]
    public async Task ProcessAsync_DispatchesCorrectWorkflowId()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("order-data", "OrderService", "order.created");

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        Assert.That(_dispatcher.LastWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    [Test]
    public async Task ProcessAsync_MapsEnvelopeFieldsToInput()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("payload-data", "TestSource", "test.type");

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = _dispatcher.LastInput;
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
        var orchestrator = CreateOrchestrator();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\":\"value\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type");

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = _dispatcher.LastInput;
        Assert.That(capturedInput!.PayloadJson, Does.Contain("key"));
        Assert.That(capturedInput.PayloadJson, Does.Contain("value"));
    }

    [Test]
    public async Task ProcessAsync_WithMetadata_SerializesMetadataJson()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("data", "Svc", "test.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-east",
                ["tenant"] = "acme",
            },
        };

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = _dispatcher.LastInput;
        Assert.That(capturedInput!.MetadataJson, Is.Not.Null);
        Assert.That(capturedInput.MetadataJson, Does.Contain("region"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("us-east"));
    }

    // ── 3. Pipeline Options ───────────────────────────────────────────

    [Test]
    public async Task ProcessAsync_EmptyMetadata_SetsMetadataJsonNull()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = _dispatcher.LastInput;
        Assert.That(capturedInput!.MetadataJson, Is.Null);
    }

    [Test]
    public async Task ProcessAsync_SetsAckAndNackSubjectsFromOptions()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        _dispatcher.ReturnsSuccess();
        await orchestrator.ProcessAsync(envelope);

        var capturedInput = _dispatcher.LastInput;
        Assert.That(capturedInput!.AckSubject, Is.EqualTo("integration.ack"));
        Assert.That(capturedInput.NackSubject, Is.EqualTo("integration.nack"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private PipelineOrchestrator CreateOrchestrator()
    {
        var options = Options.Create(new PipelineOptions
        {
            AckSubject = "integration.ack",
            NackSubject = "integration.nack",
        });
        return new PipelineOrchestrator(
            _dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);
    }

    private static IntegrationEnvelope<JsonElement> CreateEnvelope(
        string payload, string source, string messageType)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(
            $"{{\"data\":\"{payload}\"}}");
        return IntegrationEnvelope<JsonElement>.Create(json, source, messageType);
    }
}
