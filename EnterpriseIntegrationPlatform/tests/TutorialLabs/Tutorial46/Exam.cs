// ============================================================================
// Tutorial 46 – Complete Integration / Demo Pipeline (Exam)
// ============================================================================
// E2E challenges: full dispatch-to-publish flow, service activator
// request-reply, and pipeline failure handling via MockEndpoint.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial46;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullDispatchToPublish_EndToEnd()
    {
        await using var output = new MockEndpoint("e2e");
        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        dispatcher.Register<string>("order.created", async (env, _) =>
        {
            var result = IntegrationEnvelope<string>.Create(
                $"processed:{env.Payload}", "pipeline", "order.processed");
            await output.PublishAsync(result, "orders-processed");
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-001\"}", "OrderService", "order.created");
        var dispatchResult = await dispatcher.DispatchAsync(envelope);

        Assert.That(dispatchResult.Succeeded, Is.True);
        output.AssertReceivedOnTopic("orders-processed", 1);
    }

    [Test]
    public async Task Challenge2_ServiceActivator_RequestReplyFlow()
    {
        await using var output = new MockEndpoint("reply");
        var activator = new ServiceActivator(
            output, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var request = IntegrationEnvelope<string>.Create("lookup-123", "client", "query.request") with
        {
            ReplyTo = "client-replies",
        };

        var result = await activator.InvokeAsync<string, string>(
            request, (env, _) => Task.FromResult<string?>($"found:{env.Payload}"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        output.AssertReceivedOnTopic("client-replies", 1);
    }

    [Test]
    public async Task Challenge3_PipelineFailure_HandledGracefully()
    {
        var temporal = Substitute.For<ITemporalWorkflowDispatcher>();
        temporal.DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(Guid.NewGuid(), false, "Temporal unavailable"));

        var orchestrator = new PipelineOrchestrator(
            temporal, Options.Create(new PipelineOptions { AckSubject = "ack", NackSubject = "nack" }),
            NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"), "Svc", "evt");

        Assert.DoesNotThrowAsync(() => orchestrator.ProcessAsync(envelope));
    }
}
