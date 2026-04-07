using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Infrastructure;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class ObservabilityServiceTests
{
    private ObservabilityService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new ObservabilityService(NullLogger<ObservabilityService>.Instance);

    // ── 1. Audit Logging ──

    [Test]
    public async Task LogAuditAsync_ValidEntry_ReturnsWithId()
    {
        var entry = MakeAuditEntry();
        var created = await _sut.LogAuditAsync(entry);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Action, Is.EqualTo("CreateHomeModel"));
    }

    [Test]
    public void LogAuditAsync_EmptyAction_ThrowsArgumentException()
    {
        var entry = MakeAuditEntry() with { Action = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.LogAuditAsync(entry));
    }

    [Test]
    public void LogAuditAsync_EmptyEntityType_ThrowsArgumentException()
    {
        var entry = MakeAuditEntry() with { EntityType = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.LogAuditAsync(entry));
    }

    [Test]
    public async Task GetAuditLogAsync_ByEntity_ReturnsMatchingEntries()
    {
        var entityId = Guid.NewGuid();
        await _sut.LogAuditAsync(MakeAuditEntry() with { EntityId = entityId });
        await _sut.LogAuditAsync(MakeAuditEntry() with { EntityId = entityId, Action = "UpdateHomeModel" });
        await _sut.LogAuditAsync(MakeAuditEntry() with { EntityId = Guid.NewGuid() }); // different entity

        var results = await _sut.GetAuditLogAsync("HomeModel", entityId);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetUserAuditLogAsync_ReturnsUserEntries()
    {
        var userId = Guid.NewGuid();
        await _sut.LogAuditAsync(MakeAuditEntry() with { UserId = userId });
        await _sut.LogAuditAsync(MakeAuditEntry() with { UserId = userId, Action = "DeleteModel" });
        await _sut.LogAuditAsync(MakeAuditEntry()); // different user

        var results = await _sut.GetUserAuditLogAsync(userId);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    // ── 2. Health Checks ──

    [Test]
    public async Task RunHealthChecksAsync_ReturnsAllComponents()
    {
        var results = await _sut.RunHealthChecksAsync();

        Assert.That(results, Has.Count.GreaterThanOrEqualTo(10));
        Assert.That(results.All(r => r.Status == HealthStatus.Healthy), Is.True);
    }

    // ── 3. Metrics ──

    [Test]
    public async Task RecordMetricAsync_ValidMetric_Succeeds()
    {
        await _sut.RecordMetricAsync("api.requests", 42);
        var metrics = await _sut.GetMetricsAsync("api.requests");

        Assert.That(metrics, Has.Count.EqualTo(1));
        Assert.That(metrics[0].Value, Is.EqualTo(42));
    }

    [Test]
    public void RecordMetricAsync_EmptyName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RecordMetricAsync("", 1));
    }

    [Test]
    public async Task GetMetricsAsync_MultipleValues_ReturnsChrono()
    {
        await _sut.RecordMetricAsync("latency", 10);
        await _sut.RecordMetricAsync("latency", 20);
        await _sut.RecordMetricAsync("latency", 30);

        var metrics = await _sut.GetMetricsAsync("latency");
        Assert.That(metrics, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetMetricsAsync_DifferentMetrics_FiltersCorrectly()
    {
        await _sut.RecordMetricAsync("cpu", 50);
        await _sut.RecordMetricAsync("memory", 70);

        var cpuMetrics = await _sut.GetMetricsAsync("cpu");
        Assert.That(cpuMetrics, Has.Count.EqualTo(1));
    }

    private static AuditLogEntry MakeAuditEntry() => new(
        Guid.Empty, "CreateHomeModel", "HomeModel", Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), null, default);
}
