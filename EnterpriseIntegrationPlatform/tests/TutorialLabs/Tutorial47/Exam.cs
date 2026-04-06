// ============================================================================
// Tutorial 47 – Saga Compensation (Exam)
// ============================================================================
// E2E challenges: multi-step compensation flow, partial failure handling,
// and saga workflow type verification via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_MultiStepCompensation_AllNotified()
    {
        await using var output = new MockEndpoint("saga-all");
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var corrId = Guid.NewGuid();
        var steps = new[] { "validate", "persist", "route", "notify", "ack" };

        foreach (var step in steps)
        {
            var ok = await svc.CompensateAsync(corrId, step);
            Assert.That(ok, Is.True);
            await output.PublishAsync(
                IntegrationEnvelope<string>.Create($"done:{step}", "saga", "saga.step.done"),
                "saga-done");
        }

        output.AssertReceivedCount(5);
    }

    [Test]
    public async Task Challenge2_PartialFailure_FailureNotificationPublished()
    {
        await using var output = new MockEndpoint("saga-partial");
        var mock = Substitute.For<ICompensationActivityService>();
        mock.CompensateAsync(Arg.Any<Guid>(), "step-1").Returns(true);
        mock.CompensateAsync(Arg.Any<Guid>(), "step-2").Returns(false);
        mock.CompensateAsync(Arg.Any<Guid>(), "step-3").Returns(true);

        var corrId = Guid.NewGuid();
        var failed = new List<string>();

        foreach (var step in new[] { "step-1", "step-2", "step-3" })
        {
            var ok = await mock.CompensateAsync(corrId, step);
            var topic = ok ? "saga-ok" : "saga-fail";
            await output.PublishAsync(
                IntegrationEnvelope<string>.Create(step, "saga", "saga.result"), topic);
            if (!ok) failed.Add(step);
        }

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("saga-ok", 2);
        output.AssertReceivedOnTopic("saga-fail", 1);
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
