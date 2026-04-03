using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.DisasterRecoveryTests;

[TestFixture]
public class DrDrillRunnerTests
{
    private DrDrillRunner _sut = null!;
    private IFailoverManager _failoverManager = null!;
    private IReplicationManager _replicationManager = null!;
    private IRecoveryPointValidator _validator = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DisasterRecoveryOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _failoverManager = Substitute.For<IFailoverManager>();
        _replicationManager = Substitute.For<IReplicationManager>();
        _validator = Substitute.For<IRecoveryPointValidator>();
        _options = new DisasterRecoveryOptions();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        _sut = new DrDrillRunner(
            _failoverManager,
            _replicationManager,
            _validator,
            NullLogger<DrDrillRunner>.Instance,
            Options.Create(_options),
            _timeProvider);
    }

    private static DrDrillScenario CreateDefaultScenario() => new()
    {
        ScenarioId = "drill-1",
        Name = "Region failure test",
        DrillType = DrDrillType.RegionFailure,
        TargetRegionId = "us-east-1",
        FailoverRegionId = "eu-west-1",
        AutoFailback = true
    };

    private void SetupSuccessfulDrill()
    {
        _failoverManager.GetAllRegionsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegionInfo>
            {
                new() { RegionId = "us-east-1", DisplayName = "US East", State = FailoverState.Primary },
                new() { RegionId = "eu-west-1", DisplayName = "EU West", State = FailoverState.Standby }
            });

        _replicationManager.GetStatusAsync("us-east-1", "eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new ReplicationStatus
            {
                SourceRegionId = "us-east-1",
                TargetRegionId = "eu-west-1",
                Lag = TimeSpan.FromSeconds(5),
                PendingItems = 5000,
                IsHealthy = true,
                CapturedAt = DateTimeOffset.UtcNow,
                LastReplicatedSequence = 95000
            });

        _failoverManager.FailoverAsync("eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new FailoverResult
            {
                Success = true,
                PromotedRegionId = "eu-west-1",
                DemotedRegionId = "us-east-1",
                Duration = TimeSpan.FromSeconds(2),
                CompletedAt = DateTimeOffset.UtcNow
            });

        _failoverManager.FailbackAsync("us-east-1", Arg.Any<CancellationToken>())
            .Returns(new FailoverResult
            {
                Success = true,
                PromotedRegionId = "us-east-1",
                DemotedRegionId = "eu-west-1",
                Duration = TimeSpan.FromSeconds(1),
                CompletedAt = DateTimeOffset.UtcNow
            });

        _validator.GetObjectivesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RecoveryObjective>());
    }

    [Test]
    public async Task RunDrillAsync_SuccessfulDrill_ReturnsSuccess()
    {
        SetupSuccessfulDrill();
        var scenario = CreateDefaultScenario();

        var result = await _sut.RunDrillAsync(scenario);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FailbackCompleted, Is.True);
        Assert.That(result.DataLoss, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task RunDrillAsync_WithObjectives_ValidatesRecovery()
    {
        SetupSuccessfulDrill();

        var objectives = new List<RecoveryObjective>
        {
            new() { ObjectiveId = "gold", Rpo = TimeSpan.FromMinutes(1), Rto = TimeSpan.FromMinutes(5) }
        };
        _validator.GetObjectivesAsync(Arg.Any<CancellationToken>()).Returns(objectives);

        var validationResult = new RecoveryPointValidationResult
        {
            Objective = objectives[0],
            RpoMet = true,
            RtoMet = true,
            CurrentLag = TimeSpan.FromSeconds(5),
            LastFailoverDuration = TimeSpan.FromSeconds(2),
            ValidatedAt = DateTimeOffset.UtcNow
        };
        _validator.ValidateAllAsync(Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<RecoveryPointValidationResult> { validationResult });

        var result = await _sut.RunDrillAsync(CreateDefaultScenario());

        Assert.That(result.ValidationResult, Is.Not.Null);
        Assert.That(result.ValidationResult!.Passed, Is.True);
    }

    [Test]
    public async Task RunDrillAsync_TargetRegionNotFound_ReturnsFailed()
    {
        _failoverManager.GetAllRegionsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegionInfo>
            {
                new() { RegionId = "eu-west-1", DisplayName = "EU West", State = FailoverState.Standby }
            });

        var result = await _sut.RunDrillAsync(CreateDefaultScenario());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task RunDrillAsync_FailoverFails_ReturnsFailed()
    {
        _failoverManager.GetAllRegionsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegionInfo>
            {
                new() { RegionId = "us-east-1", DisplayName = "US East", State = FailoverState.Primary },
                new() { RegionId = "eu-west-1", DisplayName = "EU West", State = FailoverState.Standby }
            });

        _replicationManager.GetStatusAsync("us-east-1", "eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new ReplicationStatus
            {
                SourceRegionId = "us-east-1",
                TargetRegionId = "eu-west-1",
                Lag = TimeSpan.Zero,
                PendingItems = 0,
                IsHealthy = true,
                CapturedAt = DateTimeOffset.UtcNow,
                LastReplicatedSequence = 0
            });

        _failoverManager.FailoverAsync("eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new FailoverResult
            {
                Success = false,
                PromotedRegionId = "eu-west-1",
                DemotedRegionId = "us-east-1",
                Duration = TimeSpan.Zero,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = "Network error"
            });

        var result = await _sut.RunDrillAsync(CreateDefaultScenario());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failover failed"));
    }

    [Test]
    public async Task RunDrillAsync_NoAutoFailback_SkipsFailback()
    {
        SetupSuccessfulDrill();
        var scenario = CreateDefaultScenario() with { AutoFailback = false };

        var result = await _sut.RunDrillAsync(scenario);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FailbackCompleted, Is.False);
        await _failoverManager.DidNotReceive().FailbackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void RunDrillAsync_NullScenario_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RunDrillAsync(null!));
    }

    [Test]
    public async Task RunDrillAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => _sut.RunDrillAsync(CreateDefaultScenario(), cts.Token));
    }

    [Test]
    public async Task GetDrillHistoryAsync_ReturnsResultsInReverseOrder()
    {
        SetupSuccessfulDrill();

        var scenario1 = CreateDefaultScenario() with { ScenarioId = "drill-1", Name = "First" };
        var scenario2 = CreateDefaultScenario() with { ScenarioId = "drill-2", Name = "Second" };

        await _sut.RunDrillAsync(scenario1);
        await _sut.RunDrillAsync(scenario2);

        var history = await _sut.GetDrillHistoryAsync();

        Assert.That(history, Has.Count.EqualTo(2));
        Assert.That(history[0].Scenario.Name, Is.EqualTo("Second"));
        Assert.That(history[1].Scenario.Name, Is.EqualTo("First"));
    }

    [Test]
    public async Task GetDrillHistoryAsync_LimitReturnsMaxResults()
    {
        SetupSuccessfulDrill();

        for (int i = 0; i < 5; i++)
        {
            await _sut.RunDrillAsync(CreateDefaultScenario() with { ScenarioId = $"drill-{i}" });
        }

        var history = await _sut.GetDrillHistoryAsync(limit: 3);

        Assert.That(history, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetDrillHistoryAsync_ZeroLimit_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetDrillHistoryAsync(0));
    }

    [Test]
    public async Task GetLastDrillResultAsync_NoDrills_ReturnsNull()
    {
        var result = await _sut.GetLastDrillResultAsync();
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLastDrillResultAsync_AfterDrill_ReturnsLatest()
    {
        SetupSuccessfulDrill();
        await _sut.RunDrillAsync(CreateDefaultScenario());

        var result = await _sut.GetLastDrillResultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
    }

    [Test]
    public async Task RunDrillAsync_FailbackFails_SuccessButFailbackNotCompleted()
    {
        _failoverManager.GetAllRegionsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegionInfo>
            {
                new() { RegionId = "us-east-1", DisplayName = "US East", State = FailoverState.Primary },
                new() { RegionId = "eu-west-1", DisplayName = "EU West", State = FailoverState.Standby }
            });

        _replicationManager.GetStatusAsync("us-east-1", "eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new ReplicationStatus
            {
                SourceRegionId = "us-east-1",
                TargetRegionId = "eu-west-1",
                Lag = TimeSpan.FromSeconds(5),
                PendingItems = 5000,
                IsHealthy = true,
                CapturedAt = DateTimeOffset.UtcNow,
                LastReplicatedSequence = 95000
            });

        _failoverManager.FailoverAsync("eu-west-1", Arg.Any<CancellationToken>())
            .Returns(new FailoverResult
            {
                Success = true,
                PromotedRegionId = "eu-west-1",
                DemotedRegionId = "us-east-1",
                Duration = TimeSpan.FromSeconds(2),
                CompletedAt = DateTimeOffset.UtcNow
            });

        _failoverManager.FailbackAsync("us-east-1", Arg.Any<CancellationToken>())
            .Returns(new FailoverResult
            {
                Success = false,
                PromotedRegionId = "us-east-1",
                DemotedRegionId = "eu-west-1",
                Duration = TimeSpan.Zero,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = "Failback timed out"
            });

        _validator.GetObjectivesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RecoveryObjective>());

        var result = await _sut.RunDrillAsync(CreateDefaultScenario());

        Assert.That(result.Success, Is.True);
        Assert.That(result.FailbackCompleted, Is.False);
    }
}
