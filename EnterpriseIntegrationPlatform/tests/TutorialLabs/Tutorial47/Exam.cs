// ============================================================================
// Tutorial 47 – Saga Compensation (Exam)
// ============================================================================
// E2E challenges: multi-step compensation flow, partial failure handling,
// and saga workflow type verification via NatsBrokerEndpoint
// (real NATS JetStream via Aspire).
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_MultiStepCompensation_AllNotified()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-exam-all");
        var topic = AspireFixture.UniqueTopic("t47-saga-done");
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var corrId = Guid.NewGuid();
        var steps = new[] { "validate", "persist", "route", "notify", "ack" };

        foreach (var step in steps)
        {
            var ok = await svc.CompensateAsync(corrId, step);
            Assert.That(ok, Is.True);
            await nats.PublishAsync(
                IntegrationEnvelope<string>.Create($"done:{step}", "saga", "saga.step.done"),
                topic, default);
        }

        nats.AssertReceivedCount(5);
    }

    [Test]
    public async Task Challenge2_PartialFailure_FailureNotificationPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-exam-partial");
        var okTopic = AspireFixture.UniqueTopic("t47-saga-ok");
        var failTopic = AspireFixture.UniqueTopic("t47-saga-fail");
        var mock = new MockCompensationActivityService()
            .WithStepResult("step-1", true)
            .WithStepResult("step-2", false)
            .WithStepResult("step-3", true);

        var corrId = Guid.NewGuid();
        var failed = new List<string>();

        foreach (var step in new[] { "step-1", "step-2", "step-3" })
        {
            var ok = await mock.CompensateAsync(corrId, step);
            var topic = ok ? okTopic : failTopic;
            await nats.PublishAsync(
                IntegrationEnvelope<string>.Create(step, "saga", "saga.result"), topic, default);
            if (!ok) failed.Add(step);
        }

        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(okTopic, 2);
        nats.AssertReceivedOnTopic(failTopic, 1);
        Assert.That(failed, Is.EqualTo(new[] { "step-2" }));
    }

    [Test]
    public void Challenge3_SagaWorkflowTypes_ExistInAssembly()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var types = assembly.GetTypes().Select(t => t.Name).ToList();

        Assert.That(types, Does.Contain("SagaCompensationWorkflow"));
        Assert.That(types, Does.Contain("SagaCompensationActivities"));
        Assert.That(types, Does.Contain("IntegrationPipelineWorkflow"));
    }
}
