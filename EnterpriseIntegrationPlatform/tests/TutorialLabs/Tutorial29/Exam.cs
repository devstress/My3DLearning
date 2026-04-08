// ============================================================================
// Tutorial 29 – Throttle & Rate Limiting (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Burst exhaustion permits then rejects
//   🟡 Intermediate  — Metric accumulation tracks all operations
//   🔴 Advanced      — Single token alternates permit and reject
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
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
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic env = null!;
            // TODO: var result = await throttle.AcquireAsync(...)
            dynamic result = null!;
            if (result.Permitted)
            {
                permitted++;
                // TODO: await output.PublishAsync(...)
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
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic env = null!;
            // TODO: var result = await throttle.AcquireAsync(...)
            dynamic result = null!;
            if (result.Permitted) {
                // TODO: await output.PublishAsync(...)
            }
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

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env1 = null!;
        // TODO: var r1 = await throttle.AcquireAsync(...)
        dynamic r1 = null!;
        Assert.That(r1.Permitted, Is.True);
        // TODO: await output.PublishAsync(...)

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env2 = null!;
        // TODO: var r2 = await throttle.AcquireAsync(...)
        dynamic r2 = null!;
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
#endif
