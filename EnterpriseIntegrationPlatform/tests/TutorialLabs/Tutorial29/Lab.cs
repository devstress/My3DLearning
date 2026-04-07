// ============================================================================
// Tutorial 29 – Throttle and Rate Limiting (Lab)
// ============================================================================
// EIP Pattern: Throttle.
// E2E: TokenBucketThrottle + MockEndpoint.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("throttle-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();


    // ── 1. Token Acquisition ─────────────────────────────────────────

    [Test]
    public async Task Acquire_WithTokens_IsPermitted()
    {
        using var throttle = CreateThrottle(burstCapacity: 5);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "evt");

        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.RejectionReason, Is.Null);

        await _output.PublishAsync(envelope, "permitted");
        _output.AssertReceivedOnTopic("permitted", 1);
    }

    [Test]
    public async Task Acquire_ExhaustsTokens_StillPermittedUntilEmpty()
    {
        using var throttle = CreateThrottle(burstCapacity: 3, refillRate: 0);
        var permitted = 0;

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"d{i}", "Svc", "evt");
            var result = await throttle.AcquireAsync(env);
            if (result.Permitted)
            {
                permitted++;
                await _output.PublishAsync(env, "processed");
            }
        }

        Assert.That(permitted, Is.EqualTo(3));
        _output.AssertReceivedOnTopic("processed", 3);
    }


    // ── 2. Rejection ─────────────────────────────────────────────────

    [Test]
    public async Task Acquire_RejectOnBackpressure_RejectsWhenEmpty()
    {
        using var throttle = CreateThrottle(burstCapacity: 1, refillRate: 0, rejectOnBackpressure: true);

        var env1 = IntegrationEnvelope<string>.Create("first", "Svc", "evt");
        var r1 = await throttle.AcquireAsync(env1);
        Assert.That(r1.Permitted, Is.True);
        await _output.PublishAsync(env1, "allowed");

        var env2 = IntegrationEnvelope<string>.Create("second", "Svc", "evt");
        var r2 = await throttle.AcquireAsync(env2);
        Assert.That(r2.Permitted, Is.False);
        Assert.That(r2.RejectionReason, Is.Not.Null);

        _output.AssertReceivedOnTopic("allowed", 1);
        _output.AssertReceivedCount(1);
    }

    [Test]
    public async Task AvailableTokens_DecrementsOnAcquire()
    {
        using var throttle = CreateThrottle(burstCapacity: 5, refillRate: 0);
        var initial = throttle.AvailableTokens;
        Assert.That(initial, Is.EqualTo(5));

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "evt");
        await throttle.AcquireAsync(env);
        await _output.PublishAsync(env, "topic");

        Assert.That(throttle.AvailableTokens, Is.EqualTo(4));
        _output.AssertReceivedOnTopic("topic", 1);
    }


    // ── 3. Metrics ───────────────────────────────────────────────────

    [Test]
    public async Task GetMetrics_ReflectsAcquireAndReject()
    {
        using var throttle = CreateThrottle(burstCapacity: 1, refillRate: 0, rejectOnBackpressure: true);

        var env1 = IntegrationEnvelope<string>.Create("a", "Svc", "evt");
        await throttle.AcquireAsync(env1);
        await _output.PublishAsync(env1, "ok");

        var env2 = IntegrationEnvelope<string>.Create("b", "Svc", "evt");
        await throttle.AcquireAsync(env2);

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.TotalAcquired, Is.EqualTo(1));
        Assert.That(metrics.TotalRejected, Is.EqualTo(1));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(1));

        _output.AssertReceivedOnTopic("ok", 1);
    }

    [Test]
    public async Task GetMetrics_RefillRate_MatchesConfig()
    {
        using var throttle = CreateThrottle(burstCapacity: 10, refillRate: 50);

        var env = IntegrationEnvelope<string>.Create("data", "Svc", "evt");
        await throttle.AcquireAsync(env);
        await _output.PublishAsync(env, "topic");

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.RefillRate, Is.EqualTo(50));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(10));
        _output.AssertReceivedOnTopic("topic", 1);
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
