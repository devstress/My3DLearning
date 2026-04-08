// ============================================================================
// Tutorial 28 – Competing Consumers (Exam · Assessment Challenges)
// ============================================================================
// PURPOSE: Prove you can apply the Competing Consumers pattern in realistic,
//          end-to-end scenarios that combine multiple concepts.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Progressive scale-up reaches max consumers
//   🟡 Intermediate — Zero lag returns defaults with no scaling
//   🔴 Advanced     — Backpressure at max then release cycle
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
using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

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
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);
        var backpressure = new BackpressureSignal();
        var opts = Options.Create(new CompetingConsumerOptions
        {
            TargetTopic = "topic", ConsumerGroup = "group",
            ScaleUpThreshold = 100, ScaleDownThreshold = 10,
            MaxConsumers = 4, MinConsumers = 1, CooldownMs = 0,
        });
        var orchestrator = new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, opts,
            NullLogger<CompetingConsumerOrchestrator>.Instance, TimeProvider.System);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 500, DateTimeOffset.UtcNow));

        for (var i = 0; i < 3; i++)
            await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(4));

        var envelope = IntegrationEnvelope<string>.Create($"count={scaler.CurrentCount}", "Svc", "scaled");
        await output.PublishAsync(envelope, "scale-events");
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
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);
        var backpressure = new BackpressureSignal();
        var opts = Options.Create(new CompetingConsumerOptions
        {
            TargetTopic = "topic", ConsumerGroup = "group",
            ScaleUpThreshold = 100, ScaleDownThreshold = 10,
            MaxConsumers = 5, MinConsumers = 1, CooldownMs = 0,
        });
        var orchestrator = new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, opts,
            NullLogger<CompetingConsumerOrchestrator>.Instance, TimeProvider.System);

        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(1));
        Assert.That(backpressure.IsBackpressured, Is.False);

        var envelope = IntegrationEnvelope<string>.Create("stable", "Svc", "status");
        await output.PublishAsync(envelope, "status");
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
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 3);
        var backpressure = new BackpressureSignal();
        var opts = Options.Create(new CompetingConsumerOptions
        {
            TargetTopic = "topic", ConsumerGroup = "group",
            ScaleUpThreshold = 100, ScaleDownThreshold = 10,
            MaxConsumers = 3, MinConsumers = 1, CooldownMs = 0,
        });
        var orchestrator = new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, opts,
            NullLogger<CompetingConsumerOrchestrator>.Instance, TimeProvider.System);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 5000, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.True);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 50, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.False);

        var envelope = IntegrationEnvelope<string>.Create("bp-cycle", "Svc", "bp.cycle");
        await output.PublishAsync(envelope, "bp-events");
        output.AssertReceivedOnTopic("bp-events", 1);
        output.AssertReceivedCount(1);
    }
}
