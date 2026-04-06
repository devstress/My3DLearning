// ============================================================================
// Tutorial 14 – Process Manager (Lab)
// ============================================================================
// EIP Pattern: Process Manager
// E2E: PipelineOrchestrator converts IntegrationEnvelope to pipeline input
// and dispatches to Temporal. Uses NSubstitute for ITemporalWorkflowDispatcher
// since Temporal requires a real server.
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
public sealed class Lab
{
    private ITemporalWorkflowDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp() => _dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();

    [Test]
    public async Task ProcessAsync_DispatchesCorrectWorkflowId()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("order-data", "OrderService", "order.created");

        SetupSuccessResult(envelope.MessageId);
        await orchestrator.ProcessAsync(envelope);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Is<string>(id => id == $"integration-{envelope.MessageId}"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_MapsEnvelopeFieldsToInput()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("payload-data", "TestSource", "test.type");

        IntegrationPipelineInput? capturedInput = null;
        _dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(capturedInput.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(capturedInput.Source, Is.EqualTo("TestSource"));
        Assert.That(capturedInput.MessageType, Is.EqualTo("test.type"));
    }

    [Test]
    public async Task ProcessAsync_SerializesPayloadAsJson()
    {
        var orchestrator = CreateOrchestrator();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\":\"value\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type");

        IntegrationPipelineInput? capturedInput = null;
        _dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

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

        IntegrationPipelineInput? capturedInput = null;
        _dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput!.MetadataJson, Is.Not.Null);
        Assert.That(capturedInput.MetadataJson, Does.Contain("region"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("us-east"));
    }

    [Test]
    public async Task ProcessAsync_EmptyMetadata_SetsMetadataJsonNull()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        IntegrationPipelineInput? capturedInput = null;
        _dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput!.MetadataJson, Is.Null);
    }

    [Test]
    public async Task ProcessAsync_SetsAckAndNackSubjectsFromOptions()
    {
        var orchestrator = CreateOrchestrator();
        var envelope = CreateEnvelope("data", "Svc", "test.type");

        IntegrationPipelineInput? capturedInput = null;
        _dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

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

    private void SetupSuccessResult(Guid messageId) =>
        _dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(messageId, true));
}
