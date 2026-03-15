using FluentAssertions;
using NSubstitute;
using Xunit;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

public class IntegrationActivitiesTests
{
    private readonly IMessageValidationService _validation = Substitute.For<IMessageValidationService>();
    private readonly IMessageLoggingService _logging = Substitute.For<IMessageLoggingService>();
    private readonly IntegrationActivities _sut;

    public IntegrationActivitiesTests()
    {
        _sut = new IntegrationActivities(_validation, _logging);
    }

    [Fact]
    public async Task ValidateMessageAsync_DelegatesToValidationService()
    {
        _validation.ValidateAsync("OrderCreated", """{"id":1}""")
            .Returns(MessageValidationResult.Success);

        var result = await _sut.ValidateMessageAsync("OrderCreated", """{"id":1}""");

        result.IsValid.Should().BeTrue();
        await _validation.Received(1).ValidateAsync("OrderCreated", """{"id":1}""");
    }

    [Fact]
    public async Task ValidateMessageAsync_WhenServiceReturnsFailure_PropagatesResult()
    {
        _validation.ValidateAsync("Bad", "x")
            .Returns(MessageValidationResult.Failure("invalid"));

        var result = await _sut.ValidateMessageAsync("Bad", "x");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Be("invalid");
    }

    [Fact]
    public async Task LogProcessingStageAsync_DelegatesToLoggingService()
    {
        var id = Guid.NewGuid();
        _logging.LogAsync(id, "OrderCreated", "Received")
            .Returns(Task.CompletedTask);

        await _sut.LogProcessingStageAsync(id, "OrderCreated", "Received");

        await _logging.Received(1).LogAsync(id, "OrderCreated", "Received");
    }
}
