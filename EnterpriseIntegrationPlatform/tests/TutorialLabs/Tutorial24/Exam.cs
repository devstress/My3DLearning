// ============================================================================
// Tutorial 24 – Retry Framework (Exam)
// ============================================================================
// Coding challenges: verify exactly MaxAttempts invocations, test that a
// single retry recovery carries the correct attempt count, and validate
// that the retry policy respects max-attempts = 1 (no retries).
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial24;

[TestFixture]
public sealed class Exam
{
    private static ExponentialBackoffRetryPolicy CreatePolicy(
        int maxAttempts = 3) =>
        new(
            Options.Create(new RetryOptions
            {
                MaxAttempts = maxAttempts,
                InitialDelayMs = 100,
                MaxDelayMs = 1000,
                BackoffMultiplier = 2.0,
                UseJitter = false,
            }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc: (_, _) => Task.CompletedTask);

    // ── Challenge 1: Exactly MaxAttempts Invocations ─────────────────────────

    [Test]
    public async Task Challenge1_OperationCalledExactlyMaxAttemptsTimes()
    {
        // When every attempt throws, the operation should be invoked exactly
        // MaxAttempts times — no more, no less.
        var policy = CreatePolicy(maxAttempts: 4);
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(
            _ =>
            {
                callCount++;
                throw new InvalidOperationException("boom");
            },
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(callCount, Is.EqualTo(4));
        Assert.That(result.Attempts, Is.EqualTo(4));
    }

    // ── Challenge 2: Recover On Second Attempt ──────────────────────────────

    [Test]
    public async Task Challenge2_RecoverOnSecondAttempt_ReportsCorrectAttempts()
    {
        // The operation fails once, then succeeds. Verify the result records
        // exactly 2 attempts with the correct return value.
        var policy = CreatePolicy(maxAttempts: 5);
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new TimeoutException("first attempt timeout");
                return Task.FromResult("recovered");
            },
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(2));
        Assert.That(result.Result, Is.EqualTo("recovered"));
        Assert.That(result.LastException, Is.Null);
    }

    // ── Challenge 3: MaxAttempts = 1 Means No Retries ───────────────────────

    [Test]
    public async Task Challenge3_MaxAttemptsOne_NoRetryOnFailure()
    {
        // With MaxAttempts = 1, a single failure should result in immediate
        // failure with no retries.
        var policy = CreatePolicy(maxAttempts: 1);
        var callCount = 0;

        var result = await policy.ExecuteAsync<int>(
            _ =>
            {
                callCount++;
                throw new ApplicationException("fatal");
            },
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.LastException, Is.TypeOf<ApplicationException>());
    }
}
