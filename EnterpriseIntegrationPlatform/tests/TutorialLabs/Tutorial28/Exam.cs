// ============================================================================
// Tutorial 28 – Competing Consumers (Exam)
// ============================================================================
// E2E challenges: progressive scale-up, cooldown enforcement, backpressure
// prevents scale-down.
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
    [Test]
    public async Task Challenge1_ProgressiveScaleUp_ReachesMax()
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

    [Test]
    public async Task Challenge2_ZeroLag_DefaultsReturned()
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

    [Test]
    public async Task Challenge3_BackpressureAtMax_ThenRelease()
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
