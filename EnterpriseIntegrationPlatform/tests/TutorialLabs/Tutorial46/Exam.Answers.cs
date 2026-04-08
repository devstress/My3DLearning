// ============================================================================
// Tutorial 46 – Complete End-to-End Integration (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — full dispatch to publish_ end to end
//   🟡 Intermediate — service activator_ request reply flow
//   🔴 Advanced     — pipeline failure_ handled gracefully
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial46;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_FullDispatchToPublish_EndToEnd()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t46-e2e");
        var topic = AspireFixture.UniqueTopic("t46-orders-processed");

        var dispatcher = new MessageDispatcher(
            Options.Create(new MessageDispatcherOptions()),
            NullLogger<MessageDispatcher>.Instance);

        dispatcher.Register<string>("order.created", async (env, _) =>
        {
            var result = IntegrationEnvelope<string>.Create(
                $"processed:{env.Payload}", "pipeline", "order.processed");
            await nats.PublishAsync(result, topic, default);
        });

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-001\"}", "OrderService", "order.created");
        var dispatchResult = await dispatcher.DispatchAsync(envelope);

        Assert.That(dispatchResult.Succeeded, Is.True);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Intermediate_ServiceActivator_RequestReplyFlow()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t46-reply-exam");
        var replyTopic = AspireFixture.UniqueTopic("t46-client-replies");

        var activator = new ServiceActivator(
            nats, Options.Create(new ServiceActivatorOptions()),
            NullLogger<ServiceActivator>.Instance);

        var request = IntegrationEnvelope<string>.Create("lookup-123", "client", "query.request") with
        {
            ReplyTo = replyTopic,
        };

        var result = await activator.InvokeAsync<string, string>(
            request, (env, _) => Task.FromResult<string?>($"found:{env.Payload}"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        nats.AssertReceivedOnTopic(replyTopic, 1);
    }

    [Test]
    public async Task Advanced_PipelineFailure_HandledGracefully()
    {
        var temporal = new MockTemporalWorkflowDispatcher()
            .ReturnsFailure("Temporal unavailable");

        var orchestrator = new PipelineOrchestrator(
            temporal, Options.Create(new PipelineOptions { AckSubject = "ack", NackSubject = "nack" }),
            NullLogger<PipelineOrchestrator>.Instance);

        var envelope = IntegrationEnvelope<JsonElement>.Create(
            JsonSerializer.Deserialize<JsonElement>("{}"), "Svc", "evt");

        Assert.DoesNotThrowAsync(() => orchestrator.ProcessAsync(envelope));
    }
}
