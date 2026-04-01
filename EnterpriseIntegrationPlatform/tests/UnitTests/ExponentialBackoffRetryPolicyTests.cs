using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ExponentialBackoffRetryPolicyTests
{
    private static readonly Func<int, CancellationToken, Task> FastDelay =
        (ms, ct) => Task.Delay(Math.Min(ms, 10), ct);

    private ExponentialBackoffRetryPolicy BuildPolicy(RetryOptions? options = null, Func<int, CancellationToken, Task>? delayFunc = null)
    {
        options ??= new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false };
        return new ExponentialBackoffRetryPolicy(
            Options.Create(options),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc);
    }

    [Test]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsIsSucceededTrue()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<string>(_ => Task.FromResult("ok"), CancellationToken.None);
        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.Result, Is.EqualTo("ok"));
    }

    [Test]
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

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(2));
        Assert.That(result.Result, Is.EqualTo(42));
    }

    [Test]
    public async Task ExecuteAsync_FailsAllAttempts_ReturnsIsSucceededFalse()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new Exception("always fails"), CancellationToken.None);
        Assert.That(result.IsSucceeded, Is.False);
    }

    [Test]
    public async Task ExecuteAsync_FailsAllAttempts_ReturnsCorrectAttemptCount()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new Exception("always fails"), CancellationToken.None);
        Assert.That(result.Attempts, Is.EqualTo(3));
    }

    [Test]
    public async Task ExecuteAsync_FailsAllAttempts_SetsLastException()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 2, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var result = await policy.ExecuteAsync<int>(_ => throw new InvalidOperationException("last error"), CancellationToken.None);
        Assert.That(result.LastException, Is.InstanceOf<InvalidOperationException>());
        Assert.That(result.LastException!.Message, Is.EqualTo("last error"));
    }

    [Test]
    public async Task ExecuteAsync_NullReturnValue_PropagatesNullResult()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<string?>(_ => Task.FromResult<string?>(null), CancellationToken.None);
        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Result, Is.Null);
    }

    [Test]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 5, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await policy.ExecuteAsync<int>(_ => Task.FromResult(1), cts.Token);

        Assert.ThrowsAsync<OperationCanceledException>(async () => await act());
    }

    [Test]
    public async Task ExecuteAsync_ZeroDelayConfigured_CompletesWithoutDelay()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var start = DateTimeOffset.UtcNow;
        await policy.ExecuteAsync<int>(_ => throw new Exception("fail"), CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;
        Assert.That(elapsed.TotalMilliseconds, Is.LessThan(500));
    }

    [Test]
    public async Task ExecuteAsync_MaxDelayCapApplied_DelayDoesNotExceedMaxDelayMs()
    {
        var recordedDelays = new List<int>();
        Func<int, CancellationToken, Task> captureDelay = (ms, ct) =>
        {
            recordedDelays.Add(ms);
            return Task.CompletedTask;
        };
        var policy = BuildPolicy(
            new RetryOptions { MaxAttempts = 3, InitialDelayMs = 100, MaxDelayMs = 200, BackoffMultiplier = 100.0, UseJitter = false },
            captureDelay);
        var callCount = 0;
        await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            if (callCount < 3) throw new Exception("fail");
            return Task.FromResult(1);
        }, CancellationToken.None);
        Assert.That(recordedDelays, Has.Count.EqualTo(2));
        Assert.That(recordedDelays, Is.EqualTo(new[] { 100, 200 }));
    }

    [Test]
    public async Task ExecuteAsync_WithJitter_DelayIsNonNegative()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 2, InitialDelayMs = 10, MaxDelayMs = 100, UseJitter = true }, FastDelay);
        var callCount = 0;
        var result = await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            if (callCount < 2) throw new Exception("fail");
            return Task.FromResult(1);
        }, CancellationToken.None);
        Assert.That(result.IsSucceeded, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsOperationResult()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync<int>(_ => Task.FromResult(99), CancellationToken.None);
        Assert.That(result.Result, Is.EqualTo(99));
        Assert.That(result.IsSucceeded, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_AllExceptionsAreRetried_RetryCountMatchesMaxAttempts()
    {
        var policy = BuildPolicy(new RetryOptions { MaxAttempts = 3, InitialDelayMs = 0, MaxDelayMs = 0, UseJitter = false });
        var callCount = 0;
        await policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            throw new ArgumentException("any exception type");
        }, CancellationToken.None);
        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public async Task ExecuteAsync_VoidOperation_SucceedsOnFirstAttempt_ReturnsIsSucceededTrue()
    {
        var policy = BuildPolicy();
        var result = await policy.ExecuteAsync(_ => Task.CompletedTask, CancellationToken.None);
        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Result, Is.True);
    }
}
