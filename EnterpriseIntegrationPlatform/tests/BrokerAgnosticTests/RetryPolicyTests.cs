// ============================================================================
// Broker-Agnostic EIP Tests — Retry Policy
// ============================================================================
// These tests prove that ExponentialBackoffRetryPolicy works identically
// regardless of what broker sits behind the pipeline. Retry is a transport-
// independent EIP concern — it wraps operations, not brokers.
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BrokerAgnosticTests;

[TestFixture]
public sealed class RetryPolicyTests
{
    // ── 1. Retry Success ────────────────────────────────────────────────

    [Test]
    public async Task RetryPolicy_SucceedsOnFirstAttempt_NoRetry()
    {
        var policy = CreatePolicy(maxAttempts: 3);
        int calls = 0;

        var result = await policy.ExecuteAsync<string>(async _ =>
        {
            calls++;
            return "ok";
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.Result, Is.EqualTo("ok"));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task RetryPolicy_SucceedsAfterTransientFailure()
    {
        var policy = CreatePolicy(maxAttempts: 3, initialDelayMs: 1);
        int calls = 0;

        var result = await policy.ExecuteAsync<int>(async _ =>
        {
            calls++;
            if (calls < 3) throw new TimeoutException("transient");
            return 42;
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.Result, Is.EqualTo(42));
    }

    // ── 2. Retry Exhaustion ─────────────────────────────────────────────

    [Test]
    public async Task RetryPolicy_ExhaustsAllAttempts_ReturnsFailure()
    {
        var policy = CreatePolicy(maxAttempts: 3, initialDelayMs: 1);

        var result = await policy.ExecuteAsync<string>(async _ =>
        {
            throw new InvalidOperationException("permanent failure");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.LastException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task RetryPolicy_VoidOperation_SucceedsAfterRetry()
    {
        var policy = CreatePolicy(maxAttempts: 4, initialDelayMs: 1);
        int calls = 0;

        var result = await policy.ExecuteAsync(async _ =>
        {
            calls++;
            if (calls < 2) throw new IOException("transient I/O");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(2));
    }

    // ── 3. Backoff Behaviour ────────────────────────────────────────────

    [Test]
    public async Task RetryPolicy_ExponentialBackoff_DelaysIncrease()
    {
        // Track actual delay values to verify exponential growth.
        var delays = new List<int>();
        var policy = new ExponentialBackoffRetryPolicy(
            Options.Create(new RetryOptions
            {
                MaxAttempts = 4,
                InitialDelayMs = 100,
                BackoffMultiplier = 2.0,
                MaxDelayMs = 10000,
                UseJitter = false
            }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            (ms, _) => { delays.Add(ms); return Task.CompletedTask; });

        await policy.ExecuteAsync<bool>(async _ =>
        {
            throw new Exception("always fail");
        }, CancellationToken.None);

        // 3 delays between 4 attempts: 100, 200, 400
        Assert.That(delays, Has.Count.EqualTo(3));
        Assert.That(delays[0], Is.EqualTo(100));
        Assert.That(delays[1], Is.EqualTo(200));
        Assert.That(delays[2], Is.EqualTo(400));
    }

    [Test]
    public void RetryPolicy_Cancellation_ThrowsOperationCancelled()
    {
        var policy = CreatePolicy(maxAttempts: 10, initialDelayMs: 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            policy.ExecuteAsync<string>(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                return "never";
            }, cts.Token));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ExponentialBackoffRetryPolicy CreatePolicy(
        int maxAttempts = 3, int initialDelayMs = 1) =>
        new(Options.Create(new RetryOptions
            {
                MaxAttempts = maxAttempts,
                InitialDelayMs = initialDelayMs,
                BackoffMultiplier = 2.0,
                MaxDelayMs = 10000,
                UseJitter = false
            }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            (_, _) => Task.CompletedTask);
}
