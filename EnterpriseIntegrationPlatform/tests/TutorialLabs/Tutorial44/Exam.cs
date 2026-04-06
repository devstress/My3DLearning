// ============================================================================
// Tutorial 44 – Disaster Recovery (Exam)
// ============================================================================
// Coding challenges: full DR drill, failover/failback lifecycle, and
// recovery point validation against objectives.
// ============================================================================

using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial44;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full DR Drill ──────────────────────────────────────────

    [Test]
    public async Task Challenge1_FullDrDrill_RegisterRegionsRunDrillVerifyResult()
    {
        var failoverMgr = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        var replicationMgr = new InMemoryReplicationManager(
            NullLogger<InMemoryReplicationManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        var validator = Substitute.For<IRecoveryPointValidator>();
        validator.GetObjectivesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RecoveryObjective>());

        // Register regions
        await failoverMgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1",
            DisplayName = "US East (Primary)",
            State = FailoverState.Primary,
        });

        await failoverMgr.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-west-2",
            DisplayName = "US West (Standby)",
            State = FailoverState.Standby,
        });

        // Set up replication state
        await replicationMgr.ReportSourceProgressAsync("us-east-1", 100);
        await replicationMgr.ReportReplicationAsync("us-east-1", "us-west-2", 95);

        var drillRunner = new DrDrillRunner(
            failoverMgr, replicationMgr, validator,
            NullLogger<DrDrillRunner>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        var scenario = new DrDrillScenario
        {
            ScenarioId = "drill-001",
            Name = "Region Failure Test",
            DrillType = DrDrillType.RegionFailure,
            TargetRegionId = "us-east-1",
            FailoverRegionId = "us-west-2",
            AutoFailback = false,
        };

        var result = await drillRunner.RunDrillAsync(scenario);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Scenario.Name, Is.EqualTo("Region Failure Test"));
        Assert.That(result.FailoverTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(result.CompletedAt, Is.GreaterThanOrEqualTo(result.StartedAt));

        // Verify the failover actually happened
        var primary = await failoverMgr.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("us-west-2"));
    }

    // ── Challenge 2: Failover and Failback Lifecycle ────────────────────────

    [Test]
    public async Task Challenge2_FailoverAndFailback_Lifecycle()
    {
        var manager = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "primary-region",
            DisplayName = "Primary",
            State = FailoverState.Primary,
        });

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "standby-region",
            DisplayName = "Standby",
            State = FailoverState.Standby,
        });

        // Initial state
        var initial = await manager.GetPrimaryAsync();
        Assert.That(initial!.RegionId, Is.EqualTo("primary-region"));

        // Failover: promote standby
        var failoverResult = await manager.FailoverAsync("standby-region");
        Assert.That(failoverResult.Success, Is.True);
        Assert.That(failoverResult.PromotedRegionId, Is.EqualTo("standby-region"));
        Assert.That(failoverResult.DemotedRegionId, Is.EqualTo("primary-region"));

        var afterFailover = await manager.GetPrimaryAsync();
        Assert.That(afterFailover!.RegionId, Is.EqualTo("standby-region"));

        // Failback: restore original primary
        var failbackResult = await manager.FailbackAsync("primary-region");
        Assert.That(failbackResult.Success, Is.True);

        var afterFailback = await manager.GetPrimaryAsync();
        Assert.That(afterFailback!.RegionId, Is.EqualTo("primary-region"));
    }

    // ── Challenge 3: Recovery Point Validation Against Objectives ────────────

    [Test]
    public async Task Challenge3_RecoveryPointValidation_AgainstObjectives()
    {
        var validator = Substitute.For<IRecoveryPointValidator>();

        var objective = new RecoveryObjective
        {
            ObjectiveId = "sla-platinum",
            Rpo = TimeSpan.FromMinutes(1),
            Rto = TimeSpan.FromMinutes(5),
            Description = "Platinum SLA",
        };

        validator.RegisterObjectiveAsync(objective, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        validator.GetObjectivesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RecoveryObjective> { objective });

        var validResult = new RecoveryPointValidationResult
        {
            Objective = objective,
            RpoMet = true,
            RtoMet = true,
            CurrentLag = TimeSpan.FromSeconds(30),
            LastFailoverDuration = TimeSpan.FromMinutes(2),
            ValidatedAt = DateTimeOffset.UtcNow,
        };

        validator.ValidateAsync(
                "sla-platinum",
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(2),
                Arg.Any<CancellationToken>())
            .Returns(validResult);

        // Register and retrieve objective
        await validator.RegisterObjectiveAsync(objective);
        var objectives = await validator.GetObjectivesAsync();
        Assert.That(objectives, Has.Count.EqualTo(1));
        Assert.That(objectives[0].Rpo, Is.EqualTo(TimeSpan.FromMinutes(1)));

        // Validate against current metrics
        var result = await validator.ValidateAsync(
            "sla-platinum",
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2));

        Assert.That(result.RpoMet, Is.True);
        Assert.That(result.RtoMet, Is.True);
        Assert.That(result.CurrentLag, Is.LessThan(objective.Rpo));
        Assert.That(result.LastFailoverDuration, Is.LessThan(objective.Rto));
    }
}
