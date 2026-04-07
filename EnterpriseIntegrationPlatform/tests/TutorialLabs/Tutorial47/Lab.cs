// ============================================================================
// Tutorial 47 – Saga Compensation (Lab)
// ============================================================================
// EIP Pattern: Saga / Compensation.
// E2E: Wire DefaultCompensationActivityService with NatsBrokerEndpoint
// (real NATS JetStream via Aspire) to demonstrate compensation
// notifications published after each step.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Lab
{
    // ── 1. Compensation Execution ────────────────────────────────────

    [Test]
    public async Task CompensateAsync_SingleStep_ReturnsTrue()
    {
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var result = await svc.CompensateAsync(Guid.NewGuid(), "validate");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CompensateAsync_MultipleSteps_AllReturnTrue()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-multi");
        var topic = AspireFixture.UniqueTopic("t47-compensations");
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var corrId = Guid.NewGuid();
        var steps = new[] { "persist", "notify", "route" };

        foreach (var step in steps)
        {
            var ok = await svc.CompensateAsync(corrId, step);
            Assert.That(ok, Is.True);
            // Publish compensation notification to NatsBrokerEndpoint
            var notification = IntegrationEnvelope<string>.Create(
                $"compensated:{step}", "saga", "saga.compensated");
            await nats.PublishAsync(notification, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }


    // ── 2. Failure Detection ─────────────────────────────────────────

    [Test]
    public async Task MockCompensation_FailureDetected_NackPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-failure");
        var topic = AspireFixture.UniqueTopic("t47-failures");
        var mock = new MockCompensationActivityService()
            .WithStepResult("persist", false);

        var corrId = Guid.NewGuid();
        var result = await mock.CompensateAsync(corrId, "persist");

        if (!result)
        {
            var nack = IntegrationEnvelope<string>.Create(
                "persist-failed", "saga", "saga.compensation.failed");
            await nats.PublishAsync(nack, topic, default);
        }

        Assert.That(result, Is.False);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Pipeline Result & Workflow Types ──────────────────────────

    [Test]
    public void IntegrationPipelineResult_FailureHasReason()
    {
        var result = new IntegrationPipelineResult(Guid.NewGuid(), false, "Compensation required");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Compensation required"));
    }

    [Test]
    public void SagaCompensationWorkflow_ClassExists()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "SagaCompensationWorkflow");

        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void SagaCompensationActivities_ClassExists()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "SagaCompensationActivities");

        Assert.That(type, Is.Not.Null);
    }
}
