using NSubstitute;
using NUnit.Framework;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class SagaCompensationActivitiesTests
{
    private ICompensationActivityService _compensationService = null!;
    private IMessageLoggingService _logging = null!;
    private SagaCompensationActivities _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _compensationService = Substitute.For<ICompensationActivityService>();
        _logging = Substitute.For<IMessageLoggingService>();
        _sut = new SagaCompensationActivities(_compensationService, _logging);
    }

    [Test]
    public async Task CompensateStepAsync_OnSuccess_LogsStartAndSucceeded()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "DebitAccount")
            .Returns(true);

        await _sut.CompensateStepAsync(correlationId, "DebitAccount");

        Received.InOrder(async () =>
        {
            await _logging.LogAsync(correlationId, "DebitAccount", "CompensationStarted:DebitAccount");
            await _logging.LogAsync(correlationId, "DebitAccount", "CompensationSucceeded:DebitAccount");
        });
    }

    [Test]
    public async Task CompensateStepAsync_OnFailure_LogsStartAndFailed()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "ReserveInventory")
            .Returns(false);

        await _sut.CompensateStepAsync(correlationId, "ReserveInventory");

        Received.InOrder(async () =>
        {
            await _logging.LogAsync(correlationId, "ReserveInventory", "CompensationStarted:ReserveInventory");
            await _logging.LogAsync(correlationId, "ReserveInventory", "CompensationFailed:ReserveInventory");
        });
    }

    [Test]
    public async Task CompensateStepAsync_ReturnsCompensationServiceResult()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "SendEmail")
            .Returns(true);

        var result = await _sut.CompensateStepAsync(correlationId, "SendEmail");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CompensateStepAsync_DelegatesToCompensationService()
    {
        var correlationId = Guid.NewGuid();
        _compensationService.CompensateAsync(correlationId, "UpdateLedger")
            .Returns(false);

        await _sut.CompensateStepAsync(correlationId, "UpdateLedger");

        await _compensationService.Received(1).CompensateAsync(correlationId, "UpdateLedger");
    }
}
