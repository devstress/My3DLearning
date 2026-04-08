// ============================================================================
// Tutorial 24 – Retry Framework (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Exhaust retries and capture the last exception message
//   🟡 Intermediate  — Cancellation during retry throws OperationCanceledException
//   🔴 Advanced      — Retry succeeds then publishes through full pipeline
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial24;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Exhaust retries and capture last exception ────────
    //
    // SCENARIO: A retry policy with maxAttempts=3 is given an operation that
    //           always throws with an incrementing message. After exhaustion,
    //           the failure info is published to a dead-letter topic.
    //
    // WHAT YOU PROVE: The policy returns IsSucceeded=false, Attempts=3, and
    //                 the LastException.Message matches the final attempt.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_ExhaustRetries_CapturesLastException()
    {
        await using var output = new MockEndpoint("exam-retry");
        var policy = CreatePolicy(maxAttempts: 3);
        var attempt = 0;

        // TODO: var result = await policy.ExecuteAsync(...)
        dynamic result = null!;

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.LastException!.Message, Is.EqualTo("fail-3"));

        // Publish failure info to dead-letter via MockEndpoint
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("dlq-topic", 1);
    }

    // ── 🟡 INTERMEDIATE — Cancellation during retry ────────────────────
    //
    // SCENARIO: A retry policy with maxAttempts=5 retries a failing operation.
    //           The delay function cancels the CancellationTokenSource on the
    //           first retry delay, simulating external cancellation mid-retry.
    //
    // WHAT YOU PROVE: The policy propagates OperationCanceledException when
    //                 the token is cancelled between retry attempts.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_CancellationDuringRetry_ThrowsOperationCanceled()
    {
        var policy = CreatePolicy(maxAttempts: 5);
        using var cts = new CancellationTokenSource();
        var attempt = 0;

        // Create a policy with a delayFunc that cancels on 2nd attempt
        // TODO: var optionsValue = Options.Create(...)
        dynamic optionsValue = null!;
        // TODO: Create a ExponentialBackoffRetryPolicy with appropriate configuration
        dynamic cancellablePolicy = null!;

        Assert.ThrowsAsync<OperationCanceledException>(async () => {
            // TODO: await cancellablePolicy.ExecuteAsync(...)
            });
    }

    // ── 🔴 ADVANCED — Retry success then publish full pipeline ─────────
    //
    // SCENARIO: A retry policy retries a failing operation that succeeds on
    //           attempt 3. The recovered value is then published through a
    //           MockEndpoint to simulate a full retry-then-publish pipeline.
    //
    // WHAT YOU PROVE: The entire pipeline works end-to-end: retry recovers
    //                 the value, and the downstream publish is verified.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_RetrySuccessThenPublish_FullPipeline()
    {
        await using var output = new MockEndpoint("exam-pipeline");
        var policy = CreatePolicy(maxAttempts: 4);
        var attempt = 0;

        // TODO: var result = await policy.ExecuteAsync(...)
        dynamic result = null!;

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.Result, Is.EqualTo("final-value"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("orders-out", 1);
        output.AssertReceivedCount(1);
    }

    private static ExponentialBackoffRetryPolicy CreatePolicy(int maxAttempts)
    {
        var options = Options.Create(new RetryOptions
        {
            MaxAttempts = maxAttempts,
            InitialDelayMs = 100,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 5000,
            UseJitter = false,
        });

        return new ExponentialBackoffRetryPolicy(
            options,
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc: (_, _) => Task.CompletedTask);
    }
}
#endif
