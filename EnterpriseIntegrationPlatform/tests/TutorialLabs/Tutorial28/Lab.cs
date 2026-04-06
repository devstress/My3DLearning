// ============================================================================
// Tutorial 28 – Competing Consumers (Lab)
// ============================================================================
// This lab exercises the CompetingConsumerOrchestrator, BackpressureSignal,
// InMemoryConsumerScaler, InMemoryConsumerLagMonitor, and ConsumerLagInfo.
// You will verify scaling decisions, backpressure, and cooldown behaviour.
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial28;

[TestFixture]
public sealed class Lab
{
    // ── BackpressureSignal Toggle ────────────────────────────────────────────

    [Test]
    public void BackpressureSignal_SignalAndRelease_TogglesCorrectly()
    {
        var bp = new BackpressureSignal();

        Assert.That(bp.IsBackpressured, Is.False);

        bp.Signal();
        Assert.That(bp.IsBackpressured, Is.True);

        bp.Release();
        Assert.That(bp.IsBackpressured, Is.False);
    }

    // ── InMemoryConsumerScaler Scales Up ─────────────────────────────────────

    [Test]
    public async Task InMemoryConsumerScaler_ScaleUp_IncreasesCount()
    {
        var scaler = new InMemoryConsumerScaler(
            NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 1);

        Assert.That(scaler.CurrentCount, Is.EqualTo(1));

        await scaler.ScaleAsync(3, CancellationToken.None);

        Assert.That(scaler.CurrentCount, Is.EqualTo(3));
    }

    // ── ConsumerLagInfo Record Shape ─────────────────────────────────────────

    [Test]
    public void ConsumerLagInfo_RecordProperties_AreCorrect()
    {
        var now = DateTimeOffset.UtcNow;
        var info = new ConsumerLagInfo("group-1", "orders", 500, now);

        Assert.That(info.ConsumerGroup, Is.EqualTo("group-1"));
        Assert.That(info.Topic, Is.EqualTo("orders"));
        Assert.That(info.CurrentLag, Is.EqualTo(500));
        Assert.That(info.Timestamp, Is.EqualTo(now));
    }

    // ── InMemoryConsumerLagMonitor Reports And Retrieves ─────────────────────

    [Test]
    public async Task InMemoryLagMonitor_ReportAndGet_ReturnsReportedLag()
    {
        var monitor = new InMemoryConsumerLagMonitor();
        var lag = new ConsumerLagInfo("grp", "topic", 1234, DateTimeOffset.UtcNow);

        await monitor.ReportLagAsync(lag);
        var retrieved = await monitor.GetLagAsync("topic", "grp", CancellationToken.None);

        Assert.That(retrieved.CurrentLag, Is.EqualTo(1234));
    }

    // ── Orchestrator Scales Up On High Lag ───────────────────────────────────

    [Test]
    public async Task EvaluateAndScale_HighLag_ScalesUp()
    {
        var lagMonitor = Substitute.For<IConsumerLagMonitor>();
        var scaler = Substitute.For<IConsumerScaler>();
        var backpressure = new BackpressureSignal();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        scaler.CurrentCount.Returns(1);
        lagMonitor.GetLagAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("grp", "topic", 5000, DateTimeOffset.UtcNow));

        var options = Options.Create(new CompetingConsumerOptions
        {
            MinConsumers = 1,
            MaxConsumers = 10,
            ScaleUpThreshold = 1000,
            ScaleDownThreshold = 100,
            CooldownMs = 1000,
            TargetTopic = "topic",
            ConsumerGroup = "grp",
        });

        var orchestrator = new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, options,
            NullLogger<CompetingConsumerOrchestrator>.Instance, timeProvider);

        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        await scaler.Received(1).ScaleAsync(2, Arg.Any<CancellationToken>());
    }

    // ── Orchestrator Signals Backpressure At Max ─────────────────────────────

    [Test]
    public async Task EvaluateAndScale_AtMaxConsumersWithHighLag_SignalsBackpressure()
    {
        var lagMonitor = Substitute.For<IConsumerLagMonitor>();
        var scaler = Substitute.For<IConsumerScaler>();
        var backpressure = Substitute.For<IBackpressureSignal>();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        scaler.CurrentCount.Returns(5);
        lagMonitor.GetLagAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("grp", "topic", 5000, DateTimeOffset.UtcNow));

        var options = Options.Create(new CompetingConsumerOptions
        {
            MinConsumers = 1,
            MaxConsumers = 5,
            ScaleUpThreshold = 1000,
            TargetTopic = "topic",
            ConsumerGroup = "grp",
        });

        var orchestrator = new CompetingConsumerOrchestrator(
            lagMonitor, scaler, backpressure, options,
            NullLogger<CompetingConsumerOrchestrator>.Instance, timeProvider);

        await orchestrator.EvaluateAndScaleAsync(CancellationToken.None);

        backpressure.Received(1).Signal();
        await scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── CompetingConsumerOptions Default Values ──────────────────────────────

    [Test]
    public void CompetingConsumerOptions_DefaultValues()
    {
        var opts = new CompetingConsumerOptions();

        Assert.That(opts.MinConsumers, Is.EqualTo(1));
        Assert.That(opts.MaxConsumers, Is.EqualTo(10));
        Assert.That(opts.ScaleUpThreshold, Is.EqualTo(1000));
        Assert.That(opts.ScaleDownThreshold, Is.EqualTo(100));
        Assert.That(opts.CooldownMs, Is.EqualTo(30_000));
        Assert.That(opts.TargetTopic, Is.EqualTo(string.Empty));
        Assert.That(opts.ConsumerGroup, Is.EqualTo(string.Empty));
    }
}
