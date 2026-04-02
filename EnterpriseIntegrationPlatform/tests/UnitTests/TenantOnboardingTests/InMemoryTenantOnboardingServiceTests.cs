using EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.TenantOnboardingTests;

[TestFixture]
public class InMemoryTenantOnboardingServiceTests
{
    private InMemoryTenantQuotaManager _quotaManager = null!;
    private InMemoryBrokerNamespaceProvisioner _namespaceProvisioner = null!;
    private InMemoryTenantOnboardingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _quotaManager = new InMemoryTenantQuotaManager(NullLogger<InMemoryTenantQuotaManager>.Instance);
        _namespaceProvisioner = new InMemoryBrokerNamespaceProvisioner(NullLogger<InMemoryBrokerNamespaceProvisioner>.Instance);
        _service = new InMemoryTenantOnboardingService(
            _quotaManager,
            _namespaceProvisioner,
            NullLogger<InMemoryTenantOnboardingService>.Instance);
    }

    private static TenantOnboardingRequest BuildRequest(
        string tenantId = "tenant-1",
        TenantPlan plan = TenantPlan.Standard) =>
        new(
            TenantId: tenantId,
            TenantName: "Acme Corp",
            Plan: plan,
            AdminEmail: "admin@acme.com");

    [Test]
    public async Task ProvisionAsync_ValidRequest_ReturnsActiveResult()
    {
        var result = await _service.ProvisionAsync(BuildRequest());

        Assert.That(result.Status, Is.EqualTo(OnboardingStatus.Active));
        Assert.That(result.TenantId, Is.EqualTo("tenant-1"));
        Assert.That(result.ProvisionedAt, Is.Not.Null);
        Assert.That(result.NamespaceConfig, Is.Not.Null);
        Assert.That(result.Quota, Is.Not.Null);
    }

    [Test]
    public async Task ProvisionAsync_ValidRequest_AssignsQuotaBasedOnPlan()
    {
        await _service.ProvisionAsync(BuildRequest(plan: TenantPlan.Premium));

        var quota = await _quotaManager.GetQuotaAsync("tenant-1");
        Assert.That(quota, Is.Not.Null);
        Assert.That(quota!.MaxMessagesPerDay, Is.EqualTo(1_000_000));
    }

    [Test]
    public async Task ProvisionAsync_ValidRequest_ProvisionsBrokerNamespace()
    {
        await _service.ProvisionAsync(BuildRequest());

        var ns = await _namespaceProvisioner.GetNamespaceAsync("tenant-1");
        Assert.That(ns, Is.Not.Null);
        Assert.That(ns!.NamespacePrefix, Is.EqualTo("ns-tenant-1"));
    }

    [Test]
    public async Task ProvisionAsync_DuplicateTenant_ThrowsInvalidOperationException()
    {
        await _service.ProvisionAsync(BuildRequest());

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProvisionAsync(BuildRequest()));
    }

    [Test]
    public void ProvisionAsync_NullRequest_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.ProvisionAsync(null!));
    }

    [Test]
    public void ProvisionAsync_EmptyTenantId_ThrowsArgumentException()
    {
        var request = new TenantOnboardingRequest("", "Name", TenantPlan.Free, "a@b.com");

        Assert.ThrowsAsync<ArgumentException>(
            () => _service.ProvisionAsync(request));
    }

    [Test]
    public async Task DeprovisionAsync_ActiveTenant_ReturnsDeprovisionedStatus()
    {
        await _service.ProvisionAsync(BuildRequest());

        var result = await _service.DeprovisionAsync("tenant-1");

        Assert.That(result.Status, Is.EqualTo(OnboardingStatus.Deprovisioned));
        Assert.That(result.NamespaceConfig, Is.Null);
        Assert.That(result.Quota, Is.Null);
    }

    [Test]
    public void DeprovisionAsync_UnknownTenant_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeprovisionAsync("unknown"));
    }

    [Test]
    public async Task GetStatusAsync_ExistingTenant_ReturnsResult()
    {
        await _service.ProvisionAsync(BuildRequest());

        var result = await _service.GetStatusAsync("tenant-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo(OnboardingStatus.Active));
    }

    [Test]
    public async Task GetStatusAsync_UnknownTenant_ReturnsNull()
    {
        var result = await _service.GetStatusAsync("unknown");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ProvisionAsync_EnterprisePlan_CreatesDedicatedNamespace()
    {
        var result = await _service.ProvisionAsync(BuildRequest(plan: TenantPlan.Enterprise));

        Assert.That(result.NamespaceConfig!.IsolationLevel, Is.EqualTo(IsolationLevel.Dedicated));
    }
}
