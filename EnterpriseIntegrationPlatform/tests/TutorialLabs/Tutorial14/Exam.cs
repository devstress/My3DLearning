// ============================================================================
// Tutorial 14 – Process Manager (Exam)
// ============================================================================
// E2E challenges: verify metadata serialisation, test envelope-to-input
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
    [Test]
    public async Task Challenge1_PriorityMapping_CastsEnumToInt()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        var orchestrator = CreateOrchestrator(dispatcher);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"item\":\"widget\"}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "order.created") with
        {
            Priority = MessagePriority.High,
        };

        IntegrationPipelineInput? captured = null;
        dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => captured = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        Assert.That(captured!.Priority, Is.EqualTo((int)MessagePriority.High));
    }

    [Test]
    public async Task Challenge2_IdempotentWorkflowId_DeterministicFromMessageId()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        var orchestrator = CreateOrchestrator(dispatcher);

        var json = JsonSerializer.Deserialize<JsonElement>("{\"data\":1}");
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            json, "Svc", "test.type");

        dispatcher.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        var expectedId = $"integration-{envelope.MessageId}";
        await dispatcher.Received(1).DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Is(expectedId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Challenge3_CausationIdAndTimestamp_PreservedInInput()
    {
        var dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
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

        IntegrationPipelineInput? captured = null;
        dispatcher.DispatchAsync(
            Arg.Do<IntegrationPipelineInput>(i => captured = i),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await orchestrator.ProcessAsync(envelope);

        Assert.That(captured!.CausationId, Is.EqualTo(causationId));
        Assert.That(captured.Timestamp, Is.EqualTo(timestamp));
        Assert.That(captured.SchemaVersion, Is.EqualTo(envelope.SchemaVersion));
    }

    private static PipelineOrchestrator CreateOrchestrator(
        ITemporalWorkflowDispatcher dispatcher)
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
