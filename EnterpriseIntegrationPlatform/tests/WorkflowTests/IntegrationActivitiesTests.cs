using NSubstitute;
using NUnit.Framework;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class IntegrationActivitiesTests
{
    private readonly IMessageValidationService _validation = Substitute.For<IMessageValidationService>();
    private readonly IMessageLoggingService _logging = Substitute.For<IMessageLoggingService>();
    private readonly IntegrationActivities _sut;

    public IntegrationActivitiesTests()
    {
        _sut = new IntegrationActivities(_validation, _logging);
    }

    [Test]
    public async Task ValidateMessageAsync_DelegatesToValidationService()
    {
        _validation.ValidateAsync("OrderCreated", """{"id":1}""")
            .Returns(MessageValidationResult.Success);

        var result = await _sut.ValidateMessageAsync("OrderCreated", """{"id":1}""");

        Assert.That(result.IsValid, Is.True);
        await _validation.Received(1).ValidateAsync("OrderCreated", """{"id":1}""");
    }

    [Test]
    public async Task ValidateMessageAsync_WhenServiceReturnsFailure_PropagatesResult()
    {
        _validation.ValidateAsync("Bad", "x")
            .Returns(MessageValidationResult.Failure("invalid"));

        var result = await _sut.ValidateMessageAsync("Bad", "x");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("invalid"));
    }

    [Test]
    public async Task LogProcessingStageAsync_DelegatesToLoggingService()
    {
        var id = Guid.NewGuid();
        _logging.LogAsync(id, "OrderCreated", "Received")
            .Returns(Task.CompletedTask);

        await _sut.LogProcessingStageAsync(id, "OrderCreated", "Received");

        await _logging.Received(1).LogAsync(id, "OrderCreated", "Received");
    }
}
