// ============================================================================
// Tutorial 28 – Competing Consumers (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Competing Consumers pattern
//          evaluates lag and auto-scales consumer instances.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. HighLag_ScalesUp                      — high lag triggers consumer scale-up
//   2. LowLag_ScalesDown                     — low lag triggers consumer scale-down
//   3. MaxConsumers_SignalsBackpressure       — at max consumers, backpressure is signaled
//   4. MinConsumers_DoesNotScaleBelow         — min consumer floor prevents scale-down
//   5. ModerateLag_NoScaleChange              — moderate lag keeps consumer count stable
//   6. BackpressureReleased_AfterLagDrops     — backpressure released when lag drops
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial28;

[TestFixture]
public sealed class Lab
{
    // ── 1. Scaling ───────────────────────────────────────────────────

    [Test]
    public async Task HighLag_ScalesUp()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-scaleup");
        var topic = AspireFixture.UniqueTopic("t28-scale");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 500, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create($"consumers={scaler.CurrentCount}", "Svc", "scale.up");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task LowLag_ScalesDown()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-scaledown");
        var topic = AspireFixture.UniqueTopic("t28-scale");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 3);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0, min: 1);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 5, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create($"consumers={scaler.CurrentCount}", "Svc", "scale.down");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Limits ────────────────────────────────────────────────────

    [Test]
    public async Task MaxConsumers_SignalsBackpressure()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-maxbp");
        var topic = AspireFixture.UniqueTopic("t28-bp");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 5);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 2000, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(backpressure.IsBackpressured, Is.True);
        Assert.That(scaler.CurrentCount, Is.EqualTo(5));

        var envelope = IntegrationEnvelope<string>.Create("backpressure", "Svc", "backpressure.active");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task MinConsumers_DoesNotScaleBelow()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-minscale");
        var topic = AspireFixture.UniqueTopic("t28-scale");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 2);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0, min: 2);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 0, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create("no-change", "Svc", "scale.none");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 3. Steady State ──────────────────────────────────────────────

    [Test]
    public async Task ModerateLag_NoScaleChange()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-moderate");
        var topic = AspireFixture.UniqueTopic("t28-scale");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 3);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 1000, scaleDown: 10, max: 5, cooldownMs: 0);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 500, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(3));
        Assert.That(backpressure.IsBackpressured, Is.False);

        var envelope = IntegrationEnvelope<string>.Create("stable", "Svc", "scale.stable");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task BackpressureReleased_AfterLagDrops()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t28-bprelease");
        var topic = AspireFixture.UniqueTopic("t28-bp");
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 5);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 2000, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.True);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 50, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);
        Assert.That(backpressure.IsBackpressured, Is.False);

        var envelope = IntegrationEnvelope<string>.Create("released", "Svc", "bp.released");
        await nats.PublishAsync(envelope, topic);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    private static CompetingConsumerOrchestrator CreateOrchestrator(
        InMemoryConsumerLagMonitor lagMonitor,
        InMemoryConsumerScaler scaler,
        BackpressureSignal backpressure,
        long scaleUp, long scaleDown, int max, int cooldownMs, int min = 1)
    {
        var opts = Options.Create(new CompetingConsumerOptions
        {
            TargetTopic = "topic",
            ConsumerGroup = "group",
            ScaleUpThreshold = scaleUp,
            ScaleDownThreshold = scaleDown,
            MaxConsumers = max,
            MinConsumers = min,
            CooldownMs = cooldownMs,
        });
        return new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, opts,
            NullLogger<CompetingConsumerOrchestrator>.Instance, TimeProvider.System);
    }
}
