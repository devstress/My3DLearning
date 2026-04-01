using EnterpriseIntegrationPlatform.Processing.Retry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class ExponentialBackoffRetryPolicyTests
{
    private ExponentialBackoffRetryPolicy BuildPolicy(RetryOptions? options = null)
    {
        options ??= new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false };
        return new ExponentialBackoffRetryPolicy(
            Options.Create(options),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsIsSucceededTrue()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<string>(_ => Task.FromResult("ok"), CancellationToken.None);
        result.IsSucceeded.Should().BeTrue();
        result.Attempts.Should().Be(1);
        result.Result.Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsOnSecondAttempt_ReturnsIsSucceededTrue()
    {
        var policy = BuildPolicy();
        var callCount = 0;
        var result = await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            if (callCount < 2) throw new Exception("transient");
            return Task.FromResult(42);
        }, CancellationToken.None);

        result.IsSucceeded.Should().BeTrue();
        result.Attempts.Should().Be(2);
        result.Result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_FailsAllAttempts_ReturnsIsSucceededFalse()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new Exception("always fails"), CancellationToken.None);
        result.IsSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_FailsAllAttempts_ReturnsCorrectAttemptCount()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new Exception("always fails"), CancellationToken.None);
        result.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_FailsAllAttempts_SetsLastException()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 2, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new InvalidOperationException("last error"), CancellationToken.None);
        result.LastException.Should().BeOfType<InvalidOperationException>();
        result.LastException!.Message.Should().Be("last error");
    }

    [Fact]
    public async Task ExecuteAsync_NullReturnValue_PropagatesNullResult()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<string?>(_ => Task.FromResult<string?>(null), CancellationToken.None);
        result.IsSucceeded.Should().BeTrue();
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 5, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await policy.ExecuteAsync<int>(_ => Task.FromResult(1), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_ZeroDelayConfigured_CompletesWithoutDelay()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var start = DateTimeOffset.UtcNow;
        await policy.ExecuteAsync<int>(_ => throw new Exception("fail"), CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;
        elapsed.TotalMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task ExecuteAsync_MaxDelayCapApplied_DelayDoesNotExceedMaxDelayMs()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 100, MaxDelayMs = 200, BackoffMultiplier = 100.0, UseJitter = false });
        var start = DateTimeOffset.UtcNow;
        var callCount = 0;
        await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            if (callCount < 3) throw new Exception("fail");
            return Task.FromResult(1);
        }, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;
        elapsed.TotalMilliseconds.Should().BeLessThan(3000);
    }

    [Fact]
    public async Task ExecuteAsync_WithJitter_DelayIsNonNegative()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 2, InitialDelayMs = 10, MaxDelayMs = 100, UseJitter = true });
        var callCount = 0;
        var result = await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            if (callCount < 2) throw new Exception("fail");
            return Task.FromResult(1);
        }, CancellationToken.None);
        result.IsSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsOperationResult()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<int>(_ => Task.FromResult(99), CancellationToken.None);
        result.Result.Should().Be(99);
        result.IsSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsAreRetried_RetryCountMatchesMaxAttempts()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var callCount = 0;
        await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            throw new ArgumentException("any exception type");
        }, CancellationToken.None);
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_SucceedsOnFirstAttempt_ReturnsIsSucceededTrue()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync(_ => Task.CompletedTask, CancellationToken.None);
        result.IsSucceeded.Should().BeTrue();
        result.Result.Should().BeTrue();
    }
}
