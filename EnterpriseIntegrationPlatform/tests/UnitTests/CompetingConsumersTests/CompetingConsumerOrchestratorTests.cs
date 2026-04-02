using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.CompetingConsumersTests;

[TestFixture]
public class CompetingConsumerOrchestratorTests
{
    private IConsumerLagMonitor _lagMonitor = null!;
    private IConsumerScaler _scaler = null!;
    private IBackpressureSignal _backpressure = null!;
    private CompetingConsumerOptions _options = null!;
    private FakeTimeProvider _timeProvider = null!;
    private CompetingConsumerOrchestrator _sut = null!;

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _lagMonitor = Substitute.For<IConsumerLagMonitor>();
        _scaler = Substitute.For<IConsumerScaler>();
        _backpressure = Substitute.For<IBackpressureSignal>();
        _options = new CompetingConsumerOptions
        {
            MinConsumers = 1,
            MaxConsumers = 5,
            ScaleUpThreshold = 1000,
            ScaleDownThreshold = 100,
            CooldownMs = 30_000,
            TargetTopic = "orders",
            ConsumerGroup = "group-1"
        };
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        _scaler.CurrentCount.Returns(1);

        _sut = new CompetingConsumerOrchestrator(
            _lagMonitor,
            _scaler,
            _backpressure,
            Options.Create(_options),
            NullLogger<CompetingConsumerOrchestrator>.Instance,
            _timeProvider);
    }

    [Test]
    public async Task EvaluateAndScaleAsync_LagAboveScaleUpThreshold_ScalesUp()
    {
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 2000, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.Received(1).ScaleAsync(2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_LagBelowScaleDownThreshold_ScalesDown()
    {
        _scaler.CurrentCount.Returns(3);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 50, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.Received(1).ScaleAsync(2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_LagBetweenThresholds_NoScaling()
    {
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 500, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_AtMaxConsumersAndHighLag_SignalsBackpressure()
    {
        _scaler.CurrentCount.Returns(5);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 5000, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        _backpressure.Received(1).Signal();
        await _scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_LagDrops_ReleasesBackpressure()
    {
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 500, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        _backpressure.Received(1).Release();
    }

    [Test]
    public async Task EvaluateAndScaleAsync_CooldownNotExpired_DoesNotScale()
    {
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 2000, DateTimeOffset.UtcNow));

        // First call — triggers scale
        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        // Advance time less than cooldown
        _timeProvider.Advance(TimeSpan.FromMilliseconds(10_000));

        // Second call — should be deferred
        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.Received(1).ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_CooldownExpired_ScalesAgain()
    {
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 2000, DateTimeOffset.UtcNow));

        // First call — triggers scale
        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        // Advance time past cooldown
        _timeProvider.Advance(TimeSpan.FromMilliseconds(31_000));

        // Second call — cooldown expired, should scale
        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.Received(2).ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_AtMinConsumersAndLowLag_DoesNotScaleDown()
    {
        _scaler.CurrentCount.Returns(1);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 10, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_ScaleUpDoesNotExceedMax_ClampsToMax()
    {
        _scaler.CurrentCount.Returns(5);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 5000, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        // At max — should signal backpressure, not scale
        await _scaler.DidNotReceive().ScaleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_ScaleDownDoesNotGoBelowMin_ClampsToMin()
    {
        _options.MinConsumers = 2;

        _sut = new CompetingConsumerOrchestrator(
            _lagMonitor, _scaler, _backpressure,
            Options.Create(_options),
            NullLogger<CompetingConsumerOrchestrator>.Instance,
            _timeProvider);

        _scaler.CurrentCount.Returns(3);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 10, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        await _scaler.Received(1).ScaleAsync(2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAndScaleAsync_HighLagButBelowMax_ReleasesBackpressure()
    {
        _scaler.CurrentCount.Returns(3);
        _lagMonitor.GetLagAsync("orders", "group-1", Arg.Any<CancellationToken>())
            .Returns(new ConsumerLagInfo("group-1", "orders", 2000, DateTimeOffset.UtcNow));

        await _sut.EvaluateAndScaleAsync(CancellationToken.None);

        _backpressure.Received(1).Release();
        _backpressure.DidNotReceive().Signal();
    }
}
