// ============================================================================
// Tutorial 29 – Throttle & Rate Limiting (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Burst exhaustion permits then rejects
//   🟡 Intermediate — Metric accumulation tracks all operations
//   🔴 Advanced     — Single token alternates permit and reject
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial29;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Burst exhaustion ─────────────────────────────────

    [Test]
    public async Task Starter_BurstExhaustion_PermittedThenRejected()
    {
        await using var output = new MockEndpoint("throttle-burst");
        using var throttle = CreateThrottle(burstCapacity: 3, rejectOnBackpressure: true);

        var permitted = 0;
        var rejected = 0;
        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"msg-{i}", "Svc", "evt");
            var result = await throttle.AcquireAsync(env);
            if (result.Permitted)
            {
                permitted++;
                await output.PublishAsync(env, "ok");
            }
            else
            {
                rejected++;
            }
        }

        Assert.That(permitted, Is.EqualTo(3));
        Assert.That(rejected, Is.EqualTo(2));
        output.AssertReceivedOnTopic("ok", 3);
    }

    // ── 🟡 INTERMEDIATE — Metric accumulation ────────────────────────

    [Test]
    public async Task Intermediate_MetricAccumulation_TracksAllOperations()
    {
        await using var output = new MockEndpoint("throttle-metrics");
        using var throttle = CreateThrottle(burstCapacity: 2, rejectOnBackpressure: true);

        for (var i = 0; i < 4; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"m{i}", "Svc", "evt");
            var result = await throttle.AcquireAsync(env);
            if (result.Permitted)
                await output.PublishAsync(env, "processed");
        }

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.TotalAcquired, Is.EqualTo(2));
        Assert.That(metrics.TotalRejected, Is.EqualTo(2));
        Assert.That(metrics.TotalWaitTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));

        output.AssertReceivedOnTopic("processed", 2);
    }

    // ── 🔴 ADVANCED — Single token alternate ──────────────────────────

    [Test]
    public async Task Advanced_SingleToken_AlternatePermitReject()
    {
        await using var output = new MockEndpoint("throttle-single");
        using var throttle = CreateThrottle(burstCapacity: 1, rejectOnBackpressure: true);

        var env1 = IntegrationEnvelope<string>.Create("first", "Svc", "evt");
        var r1 = await throttle.AcquireAsync(env1);
        Assert.That(r1.Permitted, Is.True);
        await output.PublishAsync(env1, "ok");

        var env2 = IntegrationEnvelope<string>.Create("second", "Svc", "evt");
        var r2 = await throttle.AcquireAsync(env2);
        Assert.That(r2.Permitted, Is.False);
        Assert.That(r2.RemainingTokens, Is.EqualTo(0));

        output.AssertReceivedCount(1);
        output.AssertReceivedOnTopic("ok", 1);
    }

    private static TokenBucketThrottle CreateThrottle(
        int burstCapacity = 10, bool rejectOnBackpressure = false)
    {
        var opts = Options.Create(new ThrottleOptions
        {
            BurstCapacity = burstCapacity,
            MaxMessagesPerSecond = 0,
            RejectOnBackpressure = rejectOnBackpressure,
            MaxWaitTime = TimeSpan.FromMilliseconds(50),
        });
        return new TokenBucketThrottle(opts, NullLogger<TokenBucketThrottle>.Instance);
    }
}
