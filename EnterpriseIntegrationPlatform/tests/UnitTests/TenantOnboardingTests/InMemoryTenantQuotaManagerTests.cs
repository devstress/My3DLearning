using EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.TenantOnboardingTests;

[TestFixture]
public class InMemoryTenantQuotaManagerTests
{
    private InMemoryTenantQuotaManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _manager = new InMemoryTenantQuotaManager(NullLogger<InMemoryTenantQuotaManager>.Instance);
    }

    [Test]
    public async Task GetQuotaAsync_NoQuotaSet_ReturnsNull()
    {
        var result = await _manager.GetQuotaAsync("tenant-1");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetQuotaAsync_ValidQuota_CanBeRetrieved()
    {
        var quota = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Standard);
        await _manager.SetQuotaAsync("tenant-1", quota);

        var result = await _manager.GetQuotaAsync("tenant-1");

        Assert.That(result, Is.EqualTo(quota));
    }

    [Test]
    public async Task SetQuotaAsync_OverwritesExisting()
    {
        var first = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Free);
        var second = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Premium);
        await _manager.SetQuotaAsync("tenant-1", first);
        await _manager.SetQuotaAsync("tenant-1", second);

        var result = await _manager.GetQuotaAsync("tenant-1");

        Assert.That(result, Is.EqualTo(second));
    }

    [Test]
    public async Task EnforceQuotaAsync_WithinLimit_ReturnsTrue()
    {
        await _manager.SetQuotaAsync("tenant-1", InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Standard));

        var allowed = await _manager.EnforceQuotaAsync("tenant-1", "messages", 50_000);

        Assert.That(allowed, Is.True);
    }

    [Test]
    public async Task EnforceQuotaAsync_ExceedsLimit_ReturnsFalse()
    {
        await _manager.SetQuotaAsync("tenant-1", InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Free));

        var allowed = await _manager.EnforceQuotaAsync("tenant-1", "messages", 5_000);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public async Task EnforceQuotaAsync_NoQuota_ReturnsFalse()
    {
        var allowed = await _manager.EnforceQuotaAsync("unknown", "messages", 1);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public async Task EnforceQuotaAsync_UnknownResourceType_ReturnsFalse()
    {
        await _manager.SetQuotaAsync("tenant-1", InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Standard));

        var allowed = await _manager.EnforceQuotaAsync("tenant-1", "unknown_resource", 1);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void GetDefaultQuota_EachPlanTier_ReturnsDistinctValues()
    {
        var free = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Free);
        var standard = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Standard);
        var premium = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Premium);
        var enterprise = InMemoryTenantQuotaManager.GetDefaultQuota(TenantPlan.Enterprise);

        Assert.That(free.MaxMessagesPerDay, Is.LessThan(standard.MaxMessagesPerDay));
        Assert.That(standard.MaxMessagesPerDay, Is.LessThan(premium.MaxMessagesPerDay));
        Assert.That(premium.MaxMessagesPerDay, Is.LessThan(enterprise.MaxMessagesPerDay));
    }
}
