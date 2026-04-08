// ============================================================================
// Tutorial 28 – Competing Consumers (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Progressive scale-up reaches max consumers
//   🟡 Intermediate  — Zero lag returns defaults with no scaling
//   🔴 Advanced      — Backpressure at max then release cycle
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial28;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Progressive scale-up ──────────────────────────────
    //
    // SCENARIO: High lag is sustained. The orchestrator evaluates repeatedly
    //           and scales up one consumer at a time until MaxConsumers.
    //
    // WHAT YOU PROVE: Progressive scale-up correctly reaches the max limit.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_ProgressiveScaleUp_ReachesMax()
    {
        await using var output = new MockEndpoint("cc-progressive");
        // TODO: Create a InMemoryConsumerLagMonitor with appropriate configuration
        dynamic lagMonitor = null!;
        // TODO: Create a InMemoryConsumerScaler with appropriate configuration
        dynamic scaler = null!;
        // TODO: Create a BackpressureSignal with appropriate configuration
        dynamic backpressure = null!;
        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a CompetingConsumerOrchestrator with appropriate configuration
        dynamic orchestrator = null!;

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 500, DateTimeOffset.UtcNow));

        for (var i = 0; i < 3; i++)
            await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(4));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("scale-events", 1);
    }

    // ── 🟡 INTERMEDIATE — Zero lag defaults ─────────────────────────────
    //
    // SCENARIO: No lag has been reported. The orchestrator evaluates and
    //           returns defaults — no scaling, no backpressure.
    //
    // WHAT YOU PROVE: The system stays stable with zero lag and default
    //                 consumer count, with no backpressure signaled.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_ZeroLag_DefaultsReturned()
    {
        await using var output = new MockEndpoint("cc-zero");
        // TODO: Create a InMemoryConsumerLagMonitor with appropriate configuration
        dynamic lagMonitor = null!;
        // TODO: Create a InMemoryConsumerScaler with appropriate configuration
        dynamic scaler = null!;
        // TODO: Create a BackpressureSignal with appropriate configuration
        dynamic backpressure = null!;
        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a CompetingConsumerOrchestrator with appropriate configuration
        dynamic orchestrator = null!;

        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(1));
        Assert.That(backpressure.IsBackpressured, Is.False);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("status", 1);
    }

    // ── 🔴 ADVANCED — Backpressure at max then release ────────────────
    //
    // SCENARIO: Consumers are at max and lag is high — backpressure is
    //           signaled. Then lag drops and backpressure is released.
    //
    // WHAT YOU PROVE: Backpressure correctly activates at capacity and
    //                 releases when lag subsides.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_BackpressureAtMax_ThenRelease()
    {
        await using var output = new MockEndpoint("cc-bp");
        // TODO: Create a InMemoryConsumerLagMonitor with appropriate configuration
        dynamic lagMonitor = null!;
        // TODO: Create a InMemoryConsumerScaler with appropriate configuration
        dynamic scaler = null!;
        // TODO: Create a BackpressureSignal with appropriate configuration
        dynamic backpressure = null!;
        // TODO: var opts = Options.Create(...)
        dynamic opts = null!;
        // TODO: Create a CompetingConsumerOrchestrator with appropriate configuration
        dynamic orchestrator = null!;

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 5000, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.True);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 50, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.False);

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("bp-events", 1);
        output.AssertReceivedCount(1);
    }
}
#endif
