// ============================================================================
// Tutorial 47 – Saga Compensation (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — multi step compensation_ all notified
//   🟡 Intermediate  — partial failure_ failure notification published
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_MultiStepCompensation_AllNotified()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-exam-all");
        var topic = AspireFixture.UniqueTopic("t47-saga-done");
        // TODO: Create a DefaultCompensationActivityService with appropriate configuration
        dynamic svc = null!;

        var corrId = Guid.NewGuid();
        var steps = new[] { "validate", "persist", "route", "notify", "ack" };

        foreach (var step in steps)
        {
            // TODO: var ok = await svc.CompensateAsync(...)
            dynamic ok = null!;
            Assert.That(ok, Is.True);
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedCount(5);
    }

    [Test]
    public async Task Intermediate_PartialFailure_FailureNotificationPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t47-exam-partial");
        var okTopic = AspireFixture.UniqueTopic("t47-saga-ok");
        var failTopic = AspireFixture.UniqueTopic("t47-saga-fail");
        // TODO: Create a MockCompensationActivityService with appropriate configuration
        dynamic mock = null!;

        var corrId = Guid.NewGuid();
        // TODO: Create a List with appropriate configuration
        dynamic failed = null!;

        foreach (var step in new[] { "step-1", "step-2", "step-3" })
        {
            // TODO: var ok = await mock.CompensateAsync(...)
            dynamic ok = null!;
            var topic = ok ? okTopic : failTopic;
            // TODO: await nats.PublishAsync(...)
            if (!ok) failed.Add(step);
        }

        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(okTopic, 2);
        nats.AssertReceivedOnTopic(failTopic, 1);
        Assert.That(failed, Is.EqualTo(new[] { "step-2" }));
    }

    [Test]
    public void Advanced_SagaWorkflowTypes_ExistInAssembly()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var types = assembly.GetTypes().Select(t => t.Name).ToList();

        Assert.That(types, Does.Contain("SagaCompensationWorkflow"));
        Assert.That(types, Does.Contain("SagaCompensationActivities"));
        Assert.That(types, Does.Contain("IntegrationPipelineWorkflow"));
    }
}
#endif
