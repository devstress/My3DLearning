using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Analytics;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class ReportingServiceTests
{
    private ReportingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var analyticsService = new AnalyticsService(NullLogger<AnalyticsService>.Instance);
        _sut = new ReportingService(analyticsService, NullLogger<ReportingService>.Instance);
    }

    // ── 1. Report Generation ──

    [Test]
    public async Task GenerateAsync_PlatformOverview_ReturnsReport()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var report = await _sut.GenerateAsync("PlatformOverview", "Monthly Overview", userId, tenantId);

        Assert.That(report.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(report.ReportType, Is.EqualTo("PlatformOverview"));
        Assert.That(report.Title, Is.EqualTo("Monthly Overview"));
        Assert.That(report.Content, Does.Contain("# Monthly Overview"));
    }

    [Test]
    public async Task GenerateAsync_JourneySummary_ReturnsReport()
    {
        var report = await _sut.GenerateAsync("JourneySummary", "Journey Report", Guid.NewGuid(), Guid.NewGuid());
        Assert.That(report.Content, Does.Contain("buyer journeys"));
    }

    [Test]
    public void GenerateAsync_UnknownReportType_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GenerateAsync("UnknownType", "Test", Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public void GenerateAsync_EmptyTitle_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GenerateAsync("PlatformOverview", "", Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public void GenerateAsync_EmptyUserId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GenerateAsync("PlatformOverview", "Test", Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void GenerateAsync_EmptyTenantId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GenerateAsync("PlatformOverview", "Test", Guid.NewGuid(), Guid.Empty));
    }

    // ── 2. Retrieval ──

    [Test]
    public async Task GetReportAsync_ExistingReport_ReturnsIt()
    {
        var report = await _sut.GenerateAsync("PlatformOverview", "Test", Guid.NewGuid(), Guid.NewGuid());
        var retrieved = await _sut.GetReportAsync(report.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Test"));
    }

    [Test]
    public async Task GetReportAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetReportAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 3. Tenant Queries & Types ──

    [Test]
    public async Task GetTenantReportsAsync_FiltersByTenant()
    {
        var tenantId = Guid.NewGuid();
        await _sut.GenerateAsync("PlatformOverview", "R1", Guid.NewGuid(), tenantId);
        await _sut.GenerateAsync("JourneySummary", "R2", Guid.NewGuid(), tenantId);
        await _sut.GenerateAsync("PlatformOverview", "R3", Guid.NewGuid(), Guid.NewGuid());

        var reports = await _sut.GetTenantReportsAsync(tenantId);
        Assert.That(reports, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAvailableReportTypesAsync_ReturnsFiveTypes()
    {
        var types = await _sut.GetAvailableReportTypesAsync();
        Assert.That(types, Has.Count.EqualTo(5));
        Assert.That(types, Does.Contain("PlatformOverview"));
        Assert.That(types, Does.Contain("JourneySummary"));
        Assert.That(types, Does.Contain("PartnerActivity"));
    }

    [Test]
    public async Task GenerateAsync_PartnerActivity_ReturnsReport()
    {
        var report = await _sut.GenerateAsync("PartnerActivity", "Partner Report", Guid.NewGuid(), Guid.NewGuid());
        Assert.That(report.Content, Does.Contain("partner engagement"));
    }
}
