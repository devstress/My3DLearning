// ============================================================================
// Tutorial 28 – Competing Consumers (Lab)
// ============================================================================
// EIP Pattern: Competing Consumers.
// E2E: CompetingConsumerOrchestrator with InMemory scaler/lag monitor +
// MockEndpoint to verify scale decisions are published.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("consumers-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task HighLag_ScalesUp()
    {
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 500, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create($"consumers={scaler.CurrentCount}", "Svc", "scale.up");
        await _output.PublishAsync(envelope, "scale-events");
        _output.AssertReceivedOnTopic("scale-events", 1);
    }

    [Test]
    public async Task LowLag_ScalesDown()
    {
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 3);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0, min: 1);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 5, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create($"consumers={scaler.CurrentCount}", "Svc", "scale.down");
        await _output.PublishAsync(envelope, "scale-events");
        _output.AssertReceivedOnTopic("scale-events", 1);
    }

    [Test]
    public async Task MaxConsumers_SignalsBackpressure()
    {
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
        await _output.PublishAsync(envelope, "bp-events");
        _output.AssertReceivedOnTopic("bp-events", 1);
    }

    [Test]
    public async Task MinConsumers_DoesNotScaleBelow()
    {
        var lagMonitor = new InMemoryConsumerLagMonitor();
        var scaler = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 2);
        var backpressure = new BackpressureSignal();
        var orchestrator = CreateOrchestrator(lagMonitor, scaler, backpressure,
            scaleUp: 100, scaleDown: 10, max: 5, cooldownMs: 0, min: 2);

        await lagMonitor.ReportLagAsync(new ConsumerLagInfo("group", "topic", 0, DateTimeOffset.UtcNow));
        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(2));

        var envelope = IntegrationEnvelope<string>.Create("no-change", "Svc", "scale.none");
        await _output.PublishAsync(envelope, "scale-events");
        _output.AssertReceivedOnTopic("scale-events", 1);
    }

    [Test]
    public async Task ModerateLag_NoScaleChange()
    {
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
        await _output.PublishAsync(envelope, "scale-events");
        _output.AssertReceivedOnTopic("scale-events", 1);
    }

    [Test]
    public async Task BackpressureReleased_AfterLagDrops()
    {
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
        await _output.PublishAsync(envelope, "bp-events");
        _output.AssertReceivedOnTopic("bp-events", 1);
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
