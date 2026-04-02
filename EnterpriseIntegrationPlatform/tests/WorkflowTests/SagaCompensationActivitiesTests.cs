using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class SagaCompensationActivitiesTests
{
    private ICompensationActivityService _compensationService = null!;
    private IMessageLoggingService _logging = null!;
    private SagaCompensationActivities _activities = null!;

    [SetUp]
    public void SetUp()
    {
        _compensationService = Substitute.For<ICompensationActivityService>();
        _logging = Substitute.For<IMessageLoggingService>();
        _activities = new SagaCompensationActivities(_compensationService, _logging);
    }

    [Test]
    public async Task CompensateStepAsync_ServiceReturnsTrue_ReturnsTrue()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "StepA")
            .Returns(Task.FromResult(true));

        var result = await _activities.CompensateStepAsync(correlationId, "StepA");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CompensateStepAsync_ServiceReturnsFalse_ReturnsFalse()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "StepB")
            .Returns(Task.FromResult(false));

        var result = await _activities.CompensateStepAsync(correlationId, "StepB");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CompensateStepAsync_LogsStartAndSuccessStages()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await _activities.CompensateStepAsync(correlationId, "StepC");

        await _logging.Received(1).LogAsync(correlationId, "StepC", "CompensationStarted:StepC");
        await _logging.Received(1).LogAsync(correlationId, "StepC", "CompensationSucceeded:StepC");
    }

    [Test]
    public async Task CompensateStepAsync_ServiceReturnsFalse_LogsFailureStage()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _activities.CompensateStepAsync(correlationId, "StepD");

        await _logging.Received(1).LogAsync(correlationId, "StepD", "CompensationFailed:StepD");
    }
}
