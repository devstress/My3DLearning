// ============================================================================
// Tutorial 24 – Retry Framework (Exam)
// ============================================================================
// E2E challenges: retry with exception chain, cancellation mid-retry,
// and retry-then-dead-letter flow via MockEndpoint.
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
    [Test]
    public async Task Challenge1_ExhaustRetries_CapturesLastException()
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

    [Test]
    public async Task Challenge2_CancellationDuringRetry_ThrowsOperationCanceled()
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

    [Test]
    public async Task Challenge3_RetrySuccessThenPublish_FullPipeline()
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
