// ============================================================================
// Tutorial 24 – Retry Framework (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Retry Framework pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Exhaust retries and capture the last exception message
//   🟡 Intermediate — Cancellation during retry throws OperationCanceledException
//   🔴 Advanced     — Retry succeeds then publishes through full pipeline
//
// HOW THIS DIFFERS FROM THE LAB:
//   • Lab tests each concept in isolation — Exam combines them
//   • Lab uses simple payloads — Exam uses realistic business domains
//   • Lab verifies one assertion — Exam verifies end-to-end flows
//   • Lab is "read and run" — Exam is "given a scenario, prove it works"
//
// INFRASTRUCTURE: MockEndpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

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

        var result = await policy.ExecuteAsync<string>(_ =>
        {
            attempt++;
            throw new InvalidOperationException($"fail-{attempt}");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.LastException!.Message, Is.EqualTo("fail-3"));

        // Publish failure info to dead-letter via MockEndpoint
        var envelope = IntegrationEnvelope<string>.Create(
            result.LastException!.Message, "svc", "retry.exhausted");
        await output.PublishAsync(envelope, "dlq-topic", CancellationToken.None);
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
        var optionsValue = Options.Create(new RetryOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 100,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 5000,
            UseJitter = false,
        });
        var cancellablePolicy = new ExponentialBackoffRetryPolicy(
            optionsValue,
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc: (_, ct) =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            });

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await cancellablePolicy.ExecuteAsync<string>(_ =>
            {
                attempt++;
                throw new Exception("transient");
            }, cts.Token));
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

        var result = await policy.ExecuteAsync<string>(_ =>
        {
            attempt++;
            if (attempt < 3) throw new Exception("not yet");
            return Task.FromResult("final-value");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.Result, Is.EqualTo("final-value"));

        var envelope = IntegrationEnvelope<string>.Create(
            result.Result!, "pipeline-svc", "order.processed");
        await output.PublishAsync(envelope, "orders-out", CancellationToken.None);
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
