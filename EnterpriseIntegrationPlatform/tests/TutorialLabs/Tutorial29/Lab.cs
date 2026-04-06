// ============================================================================
// Tutorial 29 – Throttle and Rate Limiting (Lab)
// ============================================================================
// This lab exercises the TokenBucketThrottle, demonstrating token acquisition,
// backpressure rejection, and ThrottleOptions configuration.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial29;

[TestFixture]
public sealed class Lab
{
    // ── Acquire Token Successfully ──────────────────────────────────────────

    [Test]
    public async Task AcquireAsync_WithAvailableTokens_ReturnsPermitted()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 100,
            BurstCapacity = 10,
            MaxWaitTime = TimeSpan.FromSeconds(5),
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.RejectionReason, Is.Null);
    }

    // ── Available Tokens Decreases After Acquire ────────────────────────────

    [Test]
    public async Task AcquireAsync_ConsumesToken_DecreasesAvailableCount()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 100,
            BurstCapacity = 5,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var before = throttle.AvailableTokens;

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");
        await throttle.AcquireAsync(envelope);

        Assert.That(throttle.AvailableTokens, Is.LessThan(before));
    }

    // ── Reject On Backpressure When No Tokens ───────────────────────────────

    [Test]
    public async Task AcquireAsync_NoTokensWithRejectOnBackpressure_RejectsImmediately()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 1,
            BurstCapacity = 1,
            RejectOnBackpressure = true,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

        // Consume the only token.
        await throttle.AcquireAsync(envelope);

        // Next acquire should be rejected (no tokens, reject mode).
        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.False);
        Assert.That(result.RejectionReason, Is.Not.Null.And.Not.Empty);
    }

    // ── ThrottleOptions Default Values ──────────────────────────────────────

    [Test]
    public void ThrottleOptions_Defaults_AreReasonable()
    {
        var opts = new ThrottleOptions();

        Assert.That(opts.MaxMessagesPerSecond, Is.EqualTo(100));
        Assert.That(opts.BurstCapacity, Is.EqualTo(200));
        Assert.That(opts.MaxWaitTime, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(opts.RejectOnBackpressure, Is.False);
    }

    // ── ThrottleResult Shape ────────────────────────────────────────────────

    [Test]
    public async Task ThrottleResult_ContainsExpectedFields()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 100,
            BurstCapacity = 10,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");
        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.WaitTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(result.RemainingTokens, Is.GreaterThanOrEqualTo(0));
    }

    // ── GetMetrics Returns Throttle Statistics ───────────────────────────────

    [Test]
    public async Task GetMetrics_AfterAcquire_TracksStatistics()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 100,
            BurstCapacity = 10,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");
        await throttle.AcquireAsync(envelope);

        var metrics = throttle.GetMetrics();

        Assert.That(metrics.TotalAcquired, Is.GreaterThan(0));
    }

    // ── ThrottlePartitionKey ────────────────────────────────────────────────

    [Test]
    public void ThrottlePartitionKey_ToKey_FormatsCorrectly()
    {
        var key = new ThrottlePartitionKey
        {
            TenantId = "tenant-a",
            Queue = "orders",
            Endpoint = "api/v1",
        };

        var formatted = key.ToKey();

        Assert.That(formatted, Does.Contain("tenant:tenant-a"));
        Assert.That(formatted, Does.Contain("queue:orders"));
        Assert.That(formatted, Does.Contain("endpoint:api/v1"));
    }
}
