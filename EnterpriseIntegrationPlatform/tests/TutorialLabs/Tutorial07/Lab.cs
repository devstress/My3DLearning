// ============================================================================
// Tutorial 07 – Temporal Workflows (Lab)
// ============================================================================
// EIP Pattern: Process Manager / Workflow Orchestration.
// E2E: Build AspireIntegrationTestHost with mocked ITemporalWorkflowDispatcher,
// wire PipelineOrchestrator, send envelope through orchestrator, verify dispatch.
// ============================================================================

using System.Reflection;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Lab
{
    [Test]
    public void ProcessIntegrationMessageWorkflow_Exists()
    {
        var assembly = typeof(TemporalOptions).Assembly;
        var workflowType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ProcessIntegrationMessageWorkflow");

        Assert.That(workflowType, Is.Not.Null);
        Assert.That(workflowType!.IsClass, Is.True);
    }

    [Test]
    public void TemporalOptions_HasExpectedDefaults()
    {
        var options = new TemporalOptions();

        Assert.That(options.TaskQueue, Is.EqualTo("integration-workflows"));
        Assert.That(options.Namespace, Is.EqualTo("default"));
        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
    }

    [Test]
    public async Task PipelineOrchestrator_DispatchesCorrectInput()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        IntegrationPipelineInput? capturedInput = null;

        dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, true);
            });

        var options = Options.Create(new PipelineOptions
            { AckSubject = "ack.test", NackSubject = "nack.test" });
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"orderId\":\"ORD-1\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "OrderService", "order.created");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.Source, Is.EqualTo("OrderService"));
        Assert.That(capturedInput.MessageType, Is.EqualTo("order.created"));
        Assert.That(capturedInput.AckSubject, Is.EqualTo("ack.test"));
        Assert.That(capturedInput.NackSubject, Is.EqualTo("nack.test"));
    }

    [Test]
    public async Task PipelineOrchestrator_SetsWorkflowIdFromMessageId()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        string? capturedWorkflowId = null;

        dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedWorkflowId = ci.ArgAt<string>(1);
                var input = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(input.MessageId, true);
            });

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type");

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    [Test]
    public async Task PipelineOrchestrator_SerializesPayloadAndMetadata()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        IntegrationPipelineInput? capturedInput = null;

        dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, true);
            });

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\":\"value\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type") with
        {
            Metadata = new Dictionary<string, string> { ["tenant"] = "acme" },
        };

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput!.PayloadJson, Does.Contain("key"));
        Assert.That(capturedInput.MetadataJson, Does.Contain("tenant"));
    }

    [Test]
    public async Task PipelineOrchestrator_MapsPriorityAsInt()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        IntegrationPipelineInput? capturedInput = null;

        dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedInput = ci.ArgAt<IntegrationPipelineInput>(0);
                return new IntegrationPipelineResult(capturedInput.MessageId, true);
            });

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "type") with
        {
            Priority = MessagePriority.High,
        };

        await orchestrator.ProcessAsync(envelope);

        Assert.That(capturedInput!.Priority, Is.EqualTo((int)MessagePriority.High));
    }
}
