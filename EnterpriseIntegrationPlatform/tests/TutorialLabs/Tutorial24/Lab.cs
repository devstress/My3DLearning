// ============================================================================
// Tutorial 24 – Retry Framework (Lab)
// ============================================================================
// This lab exercises the ExponentialBackoffRetryPolicy with a no-delay
// override.  You will verify success on first attempt, retry on transient
// failures, max-attempt exhaustion, and the void-returning overload.
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial24;

[TestFixture]
public sealed class Lab
{
    private static ExponentialBackoffRetryPolicy CreatePolicy(
        int maxAttempts = 3,
        int initialDelayMs = 100,
        double multiplier = 2.0) =>
        new(
            Options.Create(new RetryOptions
            {
                MaxAttempts = maxAttempts,
                InitialDelayMs = initialDelayMs,
                MaxDelayMs = 5000,
                BackoffMultiplier = multiplier,
                UseJitter = false,
            }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc: (_, _) => Task.CompletedTask);

    // ── Success On First Attempt ─────────────────────────────────────────────

    [Test]
    public async Task Execute_SuccessOnFirstAttempt_ReturnsResult()
    {
        var policy = CreatePolicy();

        var result = await policy.ExecuteAsync<int>(
            _ => Task.FromResult(42), CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.Result, Is.EqualTo(42));
        Assert.That(result.LastException, Is.Null);
    }

    // ── Retry Succeeds After Transient Failure ───────────────────────────────

    [Test]
    public async Task Execute_FailsThenSucceeds_RetriesCorrectly()
    {
        var policy = CreatePolicy(maxAttempts: 5);
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(
            _ =>
            {
                callCount++;
                if (callCount < 3)
                    throw new InvalidOperationException("transient");
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.Result, Is.EqualTo("ok"));
    }

    // ── All Attempts Exhausted ───────────────────────────────────────────────

    [Test]
    public async Task Execute_AllAttemptsFail_ReturnsFailureWithException()
    {
        var policy = CreatePolicy(maxAttempts: 3);

        var result = await policy.ExecuteAsync<string>(
            _ => throw new TimeoutException("always fails"),
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.LastException, Is.TypeOf<TimeoutException>());
        Assert.That(result.Result, Is.Null);
    }

    // ── Void Overload Returns True On Success ────────────────────────────────

    [Test]
    public async Task ExecuteVoid_SuccessOnFirst_ReturnsTrueResult()
    {
        var policy = CreatePolicy();

        var result = await policy.ExecuteAsync(
            _ => Task.CompletedTask, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.Result, Is.True);
    }

    // ── Void Overload Retries And Fails ──────────────────────────────────────

    [Test]
    public async Task ExecuteVoid_AllFail_ReturnsFailure()
    {
        var policy = CreatePolicy(maxAttempts: 2);

        var result = await policy.ExecuteAsync(
            _ => throw new IOException("disk full"),
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(2));
        Assert.That(result.LastException, Is.TypeOf<IOException>());
    }

    // ── Options Default Values ──────────────────────────────────────────────

    [Test]
    public void Options_DefaultValues_AreCorrect()
    {
        var opts = new RetryOptions();

        Assert.That(opts.MaxAttempts, Is.EqualTo(3));
        Assert.That(opts.InitialDelayMs, Is.EqualTo(1000));
        Assert.That(opts.MaxDelayMs, Is.EqualTo(30000));
        Assert.That(opts.BackoffMultiplier, Is.EqualTo(2.0));
        Assert.That(opts.UseJitter, Is.True);
    }

    // ── Cancellation Is Propagated ──────────────────────────────────────────

    [Test]
    public void Execute_CancelledToken_ThrowsOperationCancelled()
    {
        var policy = CreatePolicy(maxAttempts: 5);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            () => policy.ExecuteAsync<int>(
                _ => Task.FromResult(1), cts.Token));
    }
}
