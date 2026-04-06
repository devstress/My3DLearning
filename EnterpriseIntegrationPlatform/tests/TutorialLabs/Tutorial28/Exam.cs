// ============================================================================
// Tutorial 28 – Competing Consumers (Exam)
// ============================================================================
// Coding challenges: scale-down behaviour, lag monitor default for unknown
// topic, and cooldown prevents rapid scaling.
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial28;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Scale Down On Low Lag ──────────────────────────────────

    [Test]
    public async Task Challenge1_LowLag_ScalesDown()
    {
        var lagMonitor = Substitute.For<IConsumerLagMonitor>();
        var scaler = Substitute.For<IConsumerScaler>();
        var backpressure = new BackpressureSignal();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        scaler.CurrentCount.Returns(5);
        lagMonitor.GetLagAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("grp", "topic", 10, DateTimeOffset.UtcNow));

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

        await scaler.Received(1).ScaleAsync(4, Arg.Any<CancellationToken>());
    }

    // ── Challenge 2: Unknown Topic Returns Zero Lag ─────────────────────────

    [Test]
    public async Task Challenge2_UnknownTopic_ReturnsZeroLag()
    {
        var monitor = new InMemoryConsumerLagMonitor();

        var lag = await monitor.GetLagAsync("nonexistent", "grp", CancellationToken.None);

        Assert.That(lag.CurrentLag, Is.EqualTo(0));
        Assert.That(lag.Topic, Is.EqualTo("nonexistent"));
        Assert.That(lag.ConsumerGroup, Is.EqualTo("grp"));
    }

    // ── Challenge 3: At Min Consumers Does Not Scale Down ───────────────────

    [Test]
    public async Task Challenge3_AtMinConsumers_DoesNotScaleDown()
    {
        var lagMonitor = Substitute.For<IConsumerLagMonitor>();
        var scaler = Substitute.For<IConsumerScaler>();
        var backpressure = new BackpressureSignal();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        scaler.CurrentCount.Returns(1);
        lagMonitor.GetLagAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("grp", "topic", 10, DateTimeOffset.UtcNow));

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

        await scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
