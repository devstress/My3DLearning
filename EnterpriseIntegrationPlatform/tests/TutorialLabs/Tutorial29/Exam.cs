// ============================================================================
// Tutorial 29 – Throttle and Rate Limiting (Exam)
// ============================================================================
// Coding challenges: burst capacity exhaustion, partition key isolation,
// and metrics tracking under load.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial29;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Exhaust Burst Capacity ─────────────────────────────────

    [Test]
    public async Task Challenge1_ExhaustBurstCapacity_SubsequentAcquiresBlocked()
    {
        // Configure a small burst capacity and consume all tokens.
        // Verify that the next acquire is rejected when RejectOnBackpressure = true.
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 1,
            BurstCapacity = 3,
            RejectOnBackpressure = true,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

        // Consume all 3 burst tokens.
        for (var i = 0; i < 3; i++)
        {
            var ok = await throttle.AcquireAsync(envelope);
            Assert.That(ok.Permitted, Is.True, $"Token {i} should be permitted");
        }

        // 4th acquire should be rejected.
        var rejected = await throttle.AcquireAsync(envelope);
        Assert.That(rejected.Permitted, Is.False);
        Assert.That(rejected.RejectionReason, Is.Not.Null);
    }

    // ── Challenge 2: Global Partition Key ───────────────────────────────────

    [Test]
    public void Challenge2_GlobalPartitionKey_HasWildcards()
    {
        // The Global partition key should use wildcards for all dimensions.
        var global = ThrottlePartitionKey.Global;

        Assert.That(global.TenantId, Is.Null);
        Assert.That(global.Queue, Is.Null);
        Assert.That(global.Endpoint, Is.Null);

        var key = global.ToKey();
        Assert.That(key, Does.Contain("tenant:*"));
        Assert.That(key, Does.Contain("queue:*"));
        Assert.That(key, Does.Contain("endpoint:*"));
    }

    // ── Challenge 3: Metrics Track Rejections ───────────────────────────────

    [Test]
    public async Task Challenge3_MetricsTrackRejections_AfterExhaustion()
    {
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = 1,
            BurstCapacity = 1,
            RejectOnBackpressure = true,
        });

        using var throttle = new TokenBucketThrottle(options, NullLogger<TokenBucketThrottle>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "TestService", "test.event");

        // Consume the single token.
        await throttle.AcquireAsync(envelope);
        // This one should be rejected.
        await throttle.AcquireAsync(envelope);

        var metrics = throttle.GetMetrics();

        Assert.That(metrics.TotalAcquired, Is.EqualTo(1));
        Assert.That(metrics.TotalRejected, Is.EqualTo(1));
    }
}
