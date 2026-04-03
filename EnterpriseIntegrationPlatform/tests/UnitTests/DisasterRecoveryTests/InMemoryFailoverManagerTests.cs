using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.DisasterRecoveryTests;

[TestFixture]
public class InMemoryFailoverManagerTests
{
    private InMemoryFailoverManager _sut = null!;
    private FakeTimeProvider _timeProvider = null!;
    private DisasterRecoveryOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DisasterRecoveryOptions();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new InMemoryFailoverManager(
            NullLogger<InMemoryFailoverManager>.Instance,
            Options.Create(_options),
            _timeProvider);
    }

    [Test]
    public async Task RegisterRegionAsync_NewRegion_CanBeRetrieved()
    {
        var region = new RegionInfo
        {
            RegionId = "us-east-1",
            DisplayName = "US East",
            State = FailoverState.Primary
        };

        await _sut.RegisterRegionAsync(region);

        var regions = await _sut.GetAllRegionsAsync();
        Assert.That(regions, Has.Count.EqualTo(1));
        Assert.That(regions[0].RegionId, Is.EqualTo("us-east-1"));
    }

    [Test]
    public async Task RegisterRegionAsync_NullRegion_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RegisterRegionAsync(null!));
    }

    [Test]
    public async Task RegisterRegionAsync_DuplicateRegion_UpdatesExisting()
    {
        var region1 = new RegionInfo { RegionId = "us-east-1", DisplayName = "V1", State = FailoverState.Primary };
        var region2 = new RegionInfo { RegionId = "us-east-1", DisplayName = "V2", State = FailoverState.Standby };

        await _sut.RegisterRegionAsync(region1);
        await _sut.RegisterRegionAsync(region2);

        var regions = await _sut.GetAllRegionsAsync();
        Assert.That(regions, Has.Count.EqualTo(1));
        Assert.That(regions[0].DisplayName, Is.EqualTo("V2"));
    }

    [Test]
    public async Task GetPrimaryAsync_WithPrimary_ReturnsCorrectRegion()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "eu-west-1", DisplayName = "Standby", State = FailoverState.Standby });

        var primary = await _sut.GetPrimaryAsync();

        Assert.That(primary, Is.Not.Null);
        Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
    }

    [Test]
    public async Task GetPrimaryAsync_NoPrimary_ReturnsNull()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "eu-west-1", DisplayName = "Standby", State = FailoverState.Standby });

        var primary = await _sut.GetPrimaryAsync();

        Assert.That(primary, Is.Null);
    }

    [Test]
    public async Task FailoverAsync_ValidTarget_PromotesTarget()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "eu-west-1", DisplayName = "Standby", State = FailoverState.Standby });

        var result = await _sut.FailoverAsync("eu-west-1");

        Assert.That(result.Success, Is.True);
        Assert.That(result.PromotedRegionId, Is.EqualTo("eu-west-1"));
        Assert.That(result.DemotedRegionId, Is.EqualTo("us-east-1"));

        var primary = await _sut.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("eu-west-1"));
    }

    [Test]
    public async Task FailoverAsync_UnknownRegion_ReturnsFailed()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });

        var result = await _sut.FailoverAsync("unknown-region");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not registered"));
    }

    [Test]
    public async Task FailoverAsync_TargetIsPrimary_ReturnsFailed()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });

        var result = await _sut.FailoverAsync("us-east-1");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already the primary"));
    }

    [Test]
    public async Task FailoverAsync_NoPrimary_ReturnsFailed()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "eu-west-1", DisplayName = "Standby", State = FailoverState.Standby });

        var result = await _sut.FailoverAsync("eu-west-1");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("No current primary"));
    }

    [Test]
    public void FailoverAsync_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.FailoverAsync(null!));
        Assert.ThrowsAsync<ArgumentException>(() => _sut.FailoverAsync(""));
        Assert.ThrowsAsync<ArgumentException>(() => _sut.FailoverAsync("  "));
    }

    [Test]
    public async Task FailbackAsync_RestoresOriginalPrimary()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "eu-west-1", DisplayName = "Standby", State = FailoverState.Standby });

        await _sut.FailoverAsync("eu-west-1");
        var result = await _sut.FailbackAsync("us-east-1");

        Assert.That(result.Success, Is.True);
        var primary = await _sut.GetPrimaryAsync();
        Assert.That(primary!.RegionId, Is.EqualTo("us-east-1"));
    }

    [Test]
    public async Task UpdateHealthCheckAsync_ExistingRegion_UpdatesTimestamp()
    {
        await _sut.RegisterRegionAsync(new RegionInfo { RegionId = "us-east-1", DisplayName = "Primary", State = FailoverState.Primary });

        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        await _sut.UpdateHealthCheckAsync("us-east-1");

        var regions = await _sut.GetAllRegionsAsync();
        Assert.That(regions[0].LastHealthCheck, Is.EqualTo(_timeProvider.GetUtcNow()));
    }

    [Test]
    public async Task UpdateHealthCheckAsync_UnknownRegion_DoesNotThrow()
    {
        Assert.DoesNotThrowAsync(() => _sut.UpdateHealthCheckAsync("non-existent"));
    }

    [Test]
    public async Task GetAllRegionsAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetAllRegionsAsync(cts.Token));
    }
}
