// ============================================================================
// Tutorial 47 – Saga Compensation (Exam)
// ============================================================================
// Coding challenges: multi-step compensation, failure scenarios, and
// saga workflow verification.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Step Saga Compensation ────────────────────────────

    [Test]
    public async Task Challenge1_MultiStepCompensation_AllStepsCompensated()
    {
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var corrId = Guid.NewGuid();
        var steps = new[] { "validate", "persist", "route", "notify", "ack" };
        var results = new List<bool>();

        foreach (var step in steps)
        {
            results.Add(await svc.CompensateAsync(corrId, step));
        }

        Assert.That(results, Has.All.True);
        Assert.That(results, Has.Count.EqualTo(5));
    }

    // ── Challenge 2: Compensation Failure Scenario ──────────────────────────

    [Test]
    public async Task Challenge2_CompensationFailure_DetectedAndHandled()
    {
        var mock = Substitute.For<ICompensationActivityService>();
        mock.CompensateAsync(Arg.Any<Guid>(), "step-1").Returns(true);
        mock.CompensateAsync(Arg.Any<Guid>(), "step-2").Returns(false); // fails
        mock.CompensateAsync(Arg.Any<Guid>(), "step-3").Returns(true);

        var corrId = Guid.NewGuid();
        var compensated = new List<(string Step, bool Success)>();

        foreach (var step in new[] { "step-1", "step-2", "step-3" })
        {
            var result = await mock.CompensateAsync(corrId, step);
            compensated.Add((step, result));
        }

        Assert.That(compensated.Count(c => c.Success), Is.EqualTo(2));
        Assert.That(compensated.Single(c => !c.Success).Step, Is.EqualTo("step-2"));
    }

    // ── Challenge 3: Workflow Type Verification ─────────────────────────────

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
