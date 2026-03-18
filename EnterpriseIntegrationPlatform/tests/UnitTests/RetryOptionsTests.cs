using EnterpriseIntegrationPlatform.Processing.Retry;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class RetryOptionsTests
{
    [Fact]
    public void MaxAttempts_Default_IsThree()
    {
        var options = new RetryOptions();
        options.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void InitialDelayMs_Default_Is1000()
    {
        var options = new RetryOptions();
        options.InitialDelayMs.Should().Be(1000);
    }

    [Fact]
    public void MaxDelayMs_Default_Is30000()
    {
        var options = new RetryOptions();
        options.MaxDelayMs.Should().Be(30000);
    }

    [Fact]
    public void BackoffMultiplier_Default_IsTwo()
    {
        var options = new RetryOptions();
        options.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void UseJitter_Default_IsTrue()
    {
        var options = new RetryOptions();
        options.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 500,
            MaxDelayMs = 60000,
            BackoffMultiplier = 1.5,
            UseJitter = false
        };

        options.MaxAttempts.Should().Be(5);
        options.InitialDelayMs.Should().Be(500);
        options.MaxDelayMs.Should().Be(60000);
        options.BackoffMultiplier.Should().Be(1.5);
        options.UseJitter.Should().BeFalse();
    }
}
