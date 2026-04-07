// ============================================================================
// Tutorial 24 – Retry Framework (Lab)
// ============================================================================
// EIP Pattern: Retry / Guaranteed Delivery.
// E2E: Wire real ExponentialBackoffRetryPolicy with no-delay override,
// then publish success to MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Retry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial24;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("retry-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Basic Retry Outcomes ──────────────────────────────────────

    [Test]
    public async Task Execute_SucceedsFirstAttempt_ReturnsResult()
    {
        var policy = CreatePolicy(maxAttempts: 3);

        var result = await policy.ExecuteAsync<string>(
            _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(result.Result, Is.EqualTo("ok"));

        // Publish success to MockEndpoint
        var envelope = IntegrationEnvelope<string>.Create(result.Result!, "svc", "retry.success");
        await _output.PublishAsync(envelope, "success-topic", CancellationToken.None);
        _output.AssertReceivedOnTopic("success-topic", 1);
    }

    [Test]
    public async Task Execute_FailsThenSucceeds_RetriesCorrectly()
    {
        var policy = CreatePolicy(maxAttempts: 3);
        var attempts = 0;

        var result = await policy.ExecuteAsync<string>(_ =>
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException("transient failure");
            return Task.FromResult("recovered");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.Result, Is.EqualTo("recovered"));
    }


    // ── 2. Overloads & Configuration ─────────────────────────────────

    [Test]
    public async Task Execute_AllAttemptsFail_ReturnsFailure()
    {
        var policy = CreatePolicy(maxAttempts: 3);

        var result = await policy.ExecuteAsync<string>(
            _ => throw new InvalidOperationException("permanent"),
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));
        Assert.That(result.LastException, Is.Not.Null);
        Assert.That(result.LastException, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task Execute_VoidOverload_ReturnsRetryResultBool()
    {
        var policy = CreatePolicy(maxAttempts: 2);
        var called = false;

        var result = await policy.ExecuteAsync(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);
        Assert.That(result.Attempts, Is.EqualTo(1));
        Assert.That(called, Is.True);
    }


    // ── 3. End-to-End Integration ────────────────────────────────────

    [Test]
    public async Task Execute_RetryThenPublish_EndToEnd()
    {
        var policy = CreatePolicy(maxAttempts: 3);
        var attempt = 0;

        var result = await policy.ExecuteAsync<string>(_ =>
        {
            attempt++;
            if (attempt < 2)
                throw new Exception("fail");
            return Task.FromResult("data");
        }, CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.True);

        var envelope = IntegrationEnvelope<string>.Create(
            result.Result!, "svc", "retry.success");
        await _output.PublishAsync(envelope, "processed-topic", CancellationToken.None);
        _output.AssertReceivedOnTopic("processed-topic", 1);
    }

    [Test]
    public async Task Execute_MaxAttemptsOne_NoRetry()
    {
        var policy = CreatePolicy(maxAttempts: 1);

        var result = await policy.ExecuteAsync<string>(
            _ => throw new Exception("fail"),
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(1));
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
