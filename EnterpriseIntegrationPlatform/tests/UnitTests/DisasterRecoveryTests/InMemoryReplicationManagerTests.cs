using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.DisasterRecoveryTests;

[TestFixture]
public class InMemoryReplicationManagerTests
{
    private InMemoryReplicationManager _sut = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DisasterRecoveryOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DisasterRecoveryOptions
        {
            MaxReplicationLag = TimeSpan.FromSeconds(30),
            PerItemReplicationTime = TimeSpan.FromMilliseconds(1)
        };
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new InMemoryReplicationManager(
            NullLogger<InMemoryReplicationManager>.Instance,
            Options.Create(_options),
            _timeProvider);
    }

    [Test]
    public async Task GetStatusAsync_NoPriorReplication_ReturnsHealthyDefaults()
    {
        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");

        Assert.That(status.IsHealthy, Is.True);
        Assert.That(status.Lag, Is.EqualTo(TimeSpan.Zero));
        Assert.That(status.PendingItems, Is.EqualTo(0));
        Assert.That(status.LastReplicatedSequence, Is.EqualTo(0));
    }

    [Test]
    public void GetStatusAsync_NullSource_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetStatusAsync(null!, "eu-west-1"));
    }

    [Test]
    public void GetStatusAsync_EmptyTarget_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.GetStatusAsync("us-east-1", ""));
    }

    [Test]
    public async Task ReportReplicationAsync_UpdatesReplicatedSequence()
    {
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 100);
        await _sut.ReportSourceProgressAsync("us-east-1", 200);

        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");

        Assert.That(status.LastReplicatedSequence, Is.EqualTo(100));
        Assert.That(status.PendingItems, Is.EqualTo(100));
    }

    [Test]
    public async Task ReportReplicationAsync_HigherSequenceOnly_DoesNotRegress()
    {
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 200);
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 100);

        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");
        Assert.That(status.LastReplicatedSequence, Is.EqualTo(200));
    }

    [Test]
    public void ReportReplicationAsync_NegativeSequence_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ReportReplicationAsync("us-east-1", "eu-west-1", -1));
    }

    [Test]
    public async Task GetStatusAsync_WithLag_CalculatesCorrectly()
    {
        // Source at 30000, replicated to 1000 → lag = 29000 items × 1ms = 29s (healthy, under 30s)
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 1000);
        await _sut.ReportSourceProgressAsync("us-east-1", 30000);

        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");

        Assert.That(status.PendingItems, Is.EqualTo(29000));
        Assert.That(status.Lag, Is.EqualTo(TimeSpan.FromMilliseconds(29000)));
        Assert.That(status.IsHealthy, Is.True);
    }

    [Test]
    public async Task GetStatusAsync_ExceedsMaxLag_ReportsUnhealthy()
    {
        // Source at 50000, replicated to 1000 → lag = 49000ms = 49s (unhealthy, over 30s threshold)
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 1000);
        await _sut.ReportSourceProgressAsync("us-east-1", 50000);

        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");

        Assert.That(status.IsHealthy, Is.False);
        Assert.That(status.PendingItems, Is.EqualTo(49000));
    }

    [Test]
    public async Task GetAllStatusesAsync_ReturnsAllPairs()
    {
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 100);
        await _sut.ReportReplicationAsync("us-east-1", "ap-south-1", 50);

        var statuses = await _sut.GetAllStatusesAsync();

        Assert.That(statuses, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllStatusesAsync_Empty_ReturnsEmptyList()
    {
        var statuses = await _sut.GetAllStatusesAsync();
        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public async Task ReportSourceProgressAsync_UpdatesAllPairsForSource()
    {
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 100);
        await _sut.ReportReplicationAsync("us-east-1", "ap-south-1", 50);
        await _sut.ReportSourceProgressAsync("us-east-1", 500);

        var status1 = await _sut.GetStatusAsync("us-east-1", "eu-west-1");
        var status2 = await _sut.GetStatusAsync("us-east-1", "ap-south-1");

        Assert.That(status1.PendingItems, Is.EqualTo(400));
        Assert.That(status2.PendingItems, Is.EqualTo(450));
    }

    [Test]
    public void ReportSourceProgressAsync_NegativeSequence_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ReportSourceProgressAsync("us-east-1", -1));
    }

    [Test]
    public async Task ReportSourceProgressAsync_DoesNotRegressSequence()
    {
        await _sut.ReportReplicationAsync("us-east-1", "eu-west-1", 50);
        await _sut.ReportSourceProgressAsync("us-east-1", 500);
        await _sut.ReportSourceProgressAsync("us-east-1", 200);

        var status = await _sut.GetStatusAsync("us-east-1", "eu-west-1");
        Assert.That(status.PendingItems, Is.EqualTo(450));
    }
}
