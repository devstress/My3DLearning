// ============================================================================
// Tutorial 47 – Saga Compensation (Lab)
// ============================================================================
// This lab exercises the DefaultCompensationActivityService, saga-related
// records, and workflow types via reflection.
// ============================================================================

using EnterpriseIntegrationPlatform.Activities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial47;

[TestFixture]
public sealed class Lab
{
    // ── DefaultCompensationActivityService Compensates Successfully ──────────

    [Test]
    public async Task CompensateAsync_ReturnsTrue()
    {
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var result = await svc.CompensateAsync(Guid.NewGuid(), "validate");

        Assert.That(result, Is.True);
    }

    // ── ICompensationActivityService Interface Shape ─────────────────────────

    [Test]
    public void ICompensationActivityService_InterfaceShape()
    {
        var type = typeof(ICompensationActivityService);

        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.GetMethod("CompensateAsync"), Is.Not.Null);
    }

    // ── SagaCompensationActivities Class Exists ─────────────────────────────

    [Test]
    public void SagaCompensationActivities_ClassExists()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var type = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SagaCompensationActivities");

        Assert.That(type, Is.Not.Null);
    }

    // ── SagaCompensationWorkflow Class Exists ───────────────────────────────

    [Test]
    public void SagaCompensationWorkflow_ClassExists()
    {
        var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
        var type = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SagaCompensationWorkflow");

        Assert.That(type, Is.Not.Null);
    }

    // ── CompensateAsync With Different Step Names ────────────────────────────

    [Test]
    public async Task CompensateAsync_MultipleSteps_AllReturnTrue()
    {
        var svc = new DefaultCompensationActivityService(
            NullLogger<DefaultCompensationActivityService>.Instance);

        var corrId = Guid.NewGuid();
        var r1 = await svc.CompensateAsync(corrId, "persist");
        var r2 = await svc.CompensateAsync(corrId, "notify");
        var r3 = await svc.CompensateAsync(corrId, "route");

        Assert.That(r1, Is.True);
        Assert.That(r2, Is.True);
        Assert.That(r3, Is.True);
    }

    // ── Mock ICompensationActivityService ────────────────────────────────────

    [Test]
    public async Task Mock_CompensationService_ReturnsConfiguredResult()
    {
        var mock = Substitute.For<ICompensationActivityService>();
        mock.CompensateAsync(Arg.Any<Guid>(), "validate")
            .Returns(true);
        mock.CompensateAsync(Arg.Any<Guid>(), "persist")
            .Returns(false);

        Assert.That(await mock.CompensateAsync(Guid.NewGuid(), "validate"), Is.True);
        Assert.That(await mock.CompensateAsync(Guid.NewGuid(), "persist"), Is.False);
    }

    // ── IntegrationPipelineResult Shape ──────────────────────────────────────

    [Test]
    public void IntegrationPipelineResult_FailureHasReason()
    {
        var result = new IntegrationPipelineResult(Guid.NewGuid(), false, "Compensation required");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Compensation required"));
    }
}
