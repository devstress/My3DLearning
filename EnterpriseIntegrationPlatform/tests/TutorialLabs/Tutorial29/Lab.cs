// ============================================================================
// Tutorial 29 – Throttle and Rate Limiting (Lab)
// ============================================================================
// EIP Pattern: Throttle.
// Real Integrations: TokenBucketThrottle + NatsBrokerEndpoint (real NATS
// JetStream via Aspire).
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial29;

[TestFixture]
public sealed class Lab
{
    // ── 1. Token Acquisition ─────────────────────────────────────────

    [Test]
    public async Task Acquire_WithTokens_IsPermitted()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-permitted");
        var topic = AspireFixture.UniqueTopic("t29-permitted");
        using var throttle = CreateThrottle(burstCapacity: 5);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt");

        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.RejectionReason, Is.Null);

        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Acquire_ExhaustsTokens_StillPermittedUntilEmpty()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-exhaust");
        var topic = AspireFixture.UniqueTopic("t29-processed");
        using var throttle = CreateThrottle(burstCapacity: 3, refillRate: 0);
        var permitted = 0;

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"d{i}", "Svc", "evt");
            var result = await throttle.AcquireAsync(env);
            if (result.Permitted)
            {
                permitted++;
                await nats.PublishAsync(env, topic);
            }
        }

        Assert.That(permitted, Is.EqualTo(3));
        nats.AssertReceivedOnTopic(topic, 3);
    }


    // ── 2. Rejection ─────────────────────────────────────────────────

    [Test]
    public async Task Acquire_RejectOnBackpressure_RejectsWhenEmpty()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-reject");
        var topic = AspireFixture.UniqueTopic("t29-allowed");
        using var throttle = CreateThrottle(burstCapacity: 1, refillRate: 0, rejectOnBackpressure: true);

        var env1 = IntegrationEnvelope<string>.Create("first", "Svc", "evt");
        var r1 = await throttle.AcquireAsync(env1);
        Assert.That(r1.Permitted, Is.True);
        await nats.PublishAsync(env1, topic);

        var env2 = IntegrationEnvelope<string>.Create("second", "Svc", "evt");
        var r2 = await throttle.AcquireAsync(env2);
        Assert.That(r2.Permitted, Is.False);
        Assert.That(r2.RejectionReason, Is.Not.Null);

        nats.AssertReceivedOnTopic(topic, 1);
        nats.AssertReceivedCount(1);
    }

    [Test]
    public async Task AvailableTokens_DecrementsOnAcquire()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-decrement");
        var topic = AspireFixture.UniqueTopic("t29-topic");
        using var throttle = CreateThrottle(burstCapacity: 5, refillRate: 0);
        var initial = throttle.AvailableTokens;
        Assert.That(initial, Is.EqualTo(5));

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "evt");
        await throttle.AcquireAsync(env);
        await nats.PublishAsync(env, topic);

        Assert.That(throttle.AvailableTokens, Is.EqualTo(4));
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Metrics ───────────────────────────────────────────────────

    [Test]
    public async Task GetMetrics_ReflectsAcquireAndReject()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-metrics");
        var topic = AspireFixture.UniqueTopic("t29-ok");
        using var throttle = CreateThrottle(burstCapacity: 1, refillRate: 0, rejectOnBackpressure: true);

        var env1 = IntegrationEnvelope<string>.Create("a", "Svc", "evt");
        await throttle.AcquireAsync(env1);
        await nats.PublishAsync(env1, topic);

        var env2 = IntegrationEnvelope<string>.Create("b", "Svc", "evt");
        await throttle.AcquireAsync(env2);

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.TotalAcquired, Is.EqualTo(1));
        Assert.That(metrics.TotalRejected, Is.EqualTo(1));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(1));

        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task GetMetrics_RefillRate_MatchesConfig()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t29-refill");
        var topic = AspireFixture.UniqueTopic("t29-topic");
        using var throttle = CreateThrottle(burstCapacity: 10, refillRate: 50);

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "evt");
        await throttle.AcquireAsync(env);
        await nats.PublishAsync(env, topic);

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.RefillRate, Is.EqualTo(50));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(10));
        nats.AssertReceivedOnTopic(topic, 1);
    }

    private static TokenBucketThrottle CreateThrottle(
        int burstCapacity = 10, int refillRate = 100, bool rejectOnBackpressure = false)
    {
        var opts = Options.Create(new ThrottleOptions
        {
            BurstCapacity = burstCapacity,
            MaxMessagesPerSecond = refillRate,
            RejectOnBackpressure = rejectOnBackpressure,
            MaxWaitTime = TimeSpan.FromMilliseconds(50),
        });
        return new TokenBucketThrottle(opts, NullLogger<TokenBucketThrottle>.Instance);
    }
}
