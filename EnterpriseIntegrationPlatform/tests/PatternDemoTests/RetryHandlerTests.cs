using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Retry pattern.
/// Automatically retries failed operations with exponential back-off.
/// BizTalk equivalent: Send Port retry settings (Retry Count, Retry Interval).
/// EIP: Guaranteed Delivery / Retry (p. 122)
/// </summary>
public class RetryHandlerTests
{
    [Fact]
    public async Task Succeeds_OnFirstAttempt()
    {
        var handler = new RetryHandler(new RetryOptions
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(1),
        });

        var result = await handler.ExecuteAsync(async ct => "success");

        result.Should().Be("success");
    }

    [Fact]
    public async Task Retries_OnTransientFailure_ThenSucceeds()
    {
        var attempts = 0;
        var handler = new RetryHandler(new RetryOptions
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(1),
            BackoffMultiplier = 1.0, // No back-off for test speed
        });

        var result = await handler.ExecuteAsync<string>(async ct =>
        {
            attempts++;
            if (attempts < 3) throw new TimeoutException("transient");
            return "recovered";
        });

        result.Should().Be("recovered");
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task Throws_AfterMaxAttempts()
    {
        var handler = new RetryHandler(new RetryOptions
        {
            MaxAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(1),
        });

        var act = () => handler.ExecuteAsync<string>(async ct =>
            throw new InvalidOperationException("permanent failure"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
