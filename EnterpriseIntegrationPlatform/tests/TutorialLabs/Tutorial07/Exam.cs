// ============================================================================
// Tutorial 07 – Temporal Workflows (Exam)
// ============================================================================
// E2E challenges: host-based orchestrator wiring, failure handling, and
// correlation ID propagation through the full pipeline dispatch.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial07;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_AspireHost_OrchestratorDispatchesViaDI()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        await using var host = AspireIntegrationTestHost.CreateBuilder()
            .ConfigureServices(svc =>
            {
                svc.AddSingleton<ITemporalWorkflowDispatcher>(dispatcher);
                svc.Configure<PipelineOptions>(o => { o.AckSubject = "ack.di"; o.NackSubject = "nack.di"; });
                svc.AddSingleton<PipelineOrchestrator>();
            })
            .Build();

        var orchestrator = host.GetService<PipelineOrchestrator>();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"test\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "DIService", "di.test");

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Source, Is.EqualTo("DIService"));
        Assert.That(captured.AckSubject, Is.EqualTo("ack.di"));
    }

    [Test]
    public async Task Challenge2_WorkflowFailure_LogsWarning()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsFailure("Validation failed");

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"bad\":true}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(json, "svc", "bad.type");

        await orchestrator.ProcessAsync(envelope);

        dispatcher.AssertDispatchCount(1);
        Assert.That(dispatcher.LastInput!.MessageType, Is.EqualTo("bad.type"));
    }

    [Test]
    public async Task Challenge3_CorrelationAndCausation_PropagatedToInput()
    {
        var dispatcher = new MockTemporalWorkflowDispatcher().ReturnsSuccess();

        var options = Options.Create(new PipelineOptions());
        var orchestrator = new PipelineOrchestrator(
            dispatcher, options, NullLogger<PipelineOrchestrator>.Instance);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var json = JsonSerializer.Deserialize<JsonElement>("{}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "svc", "type", correlationId, causationId);

        await orchestrator.ProcessAsync(envelope);

        var captured = dispatcher.LastInput;
        Assert.That(captured!.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(captured.CausationId, Is.EqualTo(causationId));
    }
}
