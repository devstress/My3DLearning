// ============================================================================
// Tutorial 29 – Throttle and Rate Limiting (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Throttle pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Burst exhaustion permits then rejects
//   🟡 Intermediate — Metric accumulation tracks all operations
//   🔴 Advanced     — Single token alternates permit and reject
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
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial29;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Burst exhaustion ─────────────────────────────────
    //
    // SCENARIO: Five messages arrive but burst capacity is only 3. The first
    //           three are permitted; the remaining two are rejected.
    //
    // WHAT YOU PROVE: Token exhaustion correctly rejects excess messages.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: Four messages are sent with burst capacity of 2. Metrics
    //           must reflect both acquired and rejected totals accurately.
    //
    // WHAT YOU PROVE: ThrottleMetrics correctly accumulates acquired,
    //                 rejected, and wait-time statistics across operations.
    // ─────────────────────────────────────────────────────────────────────

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
    //
    // SCENARIO: Only one token is available. The first acquire succeeds,
    //           the second is immediately rejected with zero remaining.
    //
    // WHAT YOU PROVE: With a single token and reject-on-backpressure, the
    //                 throttle alternates between permit and reject cleanly.
    // ─────────────────────────────────────────────────────────────────────

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
