using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

public class MessageValidationResultTests
{
    [Fact]
    public void Success_ShouldBeValid()
    {
        var result = MessageValidationResult.Success;

        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldNotBeValid_AndIncludeReason()
    {
        var result = MessageValidationResult.Failure("bad payload");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Be("bad payload");
    }

    [Fact]
    public void Success_ShouldBeSameInstance()
    {
        MessageValidationResult.Success.Should().BeSameAs(MessageValidationResult.Success);
    }
}
