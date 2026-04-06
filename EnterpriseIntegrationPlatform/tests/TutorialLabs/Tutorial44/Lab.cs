// ============================================================================
// Tutorial 44 – Disaster Recovery (Lab)
// ============================================================================
// This lab exercises InMemoryFailoverManager, InMemoryReplicationManager,
// DrDrillType, FailoverResult, ReplicationStatus, RecoveryObjective, and
// DisasterRecoveryOptions records and classes.
// ============================================================================

using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial44;

[TestFixture]
public sealed class Lab
{
    // ── FailoverResult Record Shape ─────────────────────────────────────────

    [Test]
    public void FailoverResult_RecordShape()
    {
        var now = DateTimeOffset.UtcNow;
        var result = new FailoverResult
        {
            Success = true,
            PromotedRegionId = "us-west-2",
            DemotedRegionId = "us-east-1",
            Duration = TimeSpan.FromMilliseconds(150),
            CompletedAt = now,
        };

        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("us-west-2"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
        Assert.That(result.CompletedAt, Is.EqualTo(now));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    // ── ReplicationStatus Record Shape ──────────────────────────────────────

    [Test]
    public void ReplicationStatus_RecordShape()
    {
        var now = DateTimeOffset.UtcNow;
        var status = new ReplicationStatus
        {
            SourceRegionId = "us-east-1",
            TargetRegionId = "eu-west-1",
            Lag = TimeSpan.FromSeconds(5),
            PendingItems = 42,
            IsHealthy = true,
            CapturedAt = now,
            LastReplicatedSequence = 1000,
        };

        Assert.That(status.SourceRegionId, Is.EqualTo("us-east-1"));
        Assert.That(status.TargetRegionId, Is.EqualTo("eu-west-1"));
        Assert.That(status.Lag, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(status.PendingItems, Is.EqualTo(42));
        Assert.That(status.IsHealthy, Is.True);
        Assert.That(status.LastReplicatedSequence, Is.EqualTo(1000));
    }

    // ── DrDrillType Enum Values ─────────────────────────────────────────────

    [Test]
    public void DrDrillType_EnumValues()
    {
        var values = Enum.GetValues<DrDrillType>();

        Assert.That(values, Does.Contain(DrDrillType.RegionFailure));
        Assert.That(values, Does.Contain(DrDrillType.NetworkPartition));
        Assert.That(values, Does.Contain(DrDrillType.StorageFailure));
        Assert.That(values, Does.Contain(DrDrillType.BrokerFailure));
        Assert.That(values, Does.Contain(DrDrillType.PlannedFailover));
        Assert.That(values, Has.Length.EqualTo(5));
    }

    // ── InMemoryFailoverManager: Register and Get Regions ───────────────────

    [Test]
    public async Task InMemoryFailoverManager_RegisterAndGetRegions()
    {
        var manager = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1",
            DisplayName = "US East",
            State = FailoverState.Primary,
        });

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "eu-west-1",
            DisplayName = "EU West",
            State = FailoverState.Standby,
        });

        var regions = await manager.GetAllRegionsAsync();
        Assert.That(regions, Has.Count.EqualTo(2));

        var primary = await manager.GetPrimaryAsync();
        Assert.That(primary, Is.Not.Null);
        Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
        Assert.That(primary.IsPrimary, Is.True);
    }

    // ── InMemoryFailoverManager: Failover Promotes Target Region ────────────

    [Test]
    public async Task InMemoryFailoverManager_Failover_PromotesTargetRegion()
    {
        var manager = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(new DisasterRecoveryOptions()));

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-east-1",
            DisplayName = "US East",
            State = FailoverState.Primary,
        });

        await manager.RegisterRegionAsync(new RegionInfo
        {
            RegionId = "us-west-2",
            DisplayName = "US West",
            State = FailoverState.Standby,
        });

        var result = await manager.FailoverAsync("us-west-2");

        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("us-west-2"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));

        var newPrimary = await manager.GetPrimaryAsync();
        Assert.That(newPrimary!.RegionId, Is.EqualTo("us-west-2"));
    }

    // ── RecoveryObjective Record Shape ──────────────────────────────────────

    [Test]
    public void RecoveryObjective_RecordShape()
    {
        var objective = new RecoveryObjective
        {
            ObjectiveId = "sla-gold",
            Rpo = TimeSpan.FromMinutes(5),
            Rto = TimeSpan.FromMinutes(15),
            Description = "Gold SLA: 5-min RPO, 15-min RTO",
        };

        Assert.That(objective.ObjectiveId, Is.EqualTo("sla-gold"));
        Assert.That(objective.Rpo, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(objective.Rto, Is.EqualTo(TimeSpan.FromMinutes(15)));
        Assert.That(objective.Description, Does.Contain("Gold SLA"));
    }

    // ── DisasterRecoveryOptions Defaults ────────────────────────────────────

    [Test]
    public void DisasterRecoveryOptions_Defaults_MaxDrillHistorySize()
    {
        var opts = new DisasterRecoveryOptions();

        Assert.That(opts.MaxDrillHistorySize, Is.EqualTo(100));
        Assert.That(DisasterRecoveryOptions.SectionName, Is.EqualTo("DisasterRecovery"));
    }
}
