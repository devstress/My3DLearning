using EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.TenantOnboardingTests;

[TestFixture]
public class InMemoryBrokerNamespaceProvisionerTests
{
    private InMemoryBrokerNamespaceProvisioner _provisioner = null!;

    [SetUp]
    public void SetUp()
    {
        _provisioner = new InMemoryBrokerNamespaceProvisioner(
            NullLogger<InMemoryBrokerNamespaceProvisioner>.Instance);
    }

    [Test]
    public async Task ProvisionNamespaceAsync_ValidConfig_ReturnsConfig()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("tenant-1", TenantPlan.Standard);

        var result = await _provisioner.ProvisionNamespaceAsync("tenant-1", config);

        Assert.That(result, Is.EqualTo(config));
    }

    [Test]
    public async Task ProvisionNamespaceAsync_DuplicateTenant_ThrowsInvalidOperationException()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("tenant-1", TenantPlan.Standard);
        await _provisioner.ProvisionNamespaceAsync("tenant-1", config);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _provisioner.ProvisionNamespaceAsync("tenant-1", config));
    }

    [Test]
    public async Task GetNamespaceAsync_Provisioned_ReturnsConfig()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("tenant-1", TenantPlan.Standard);
        await _provisioner.ProvisionNamespaceAsync("tenant-1", config);

        var result = await _provisioner.GetNamespaceAsync("tenant-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TenantId, Is.EqualTo("tenant-1"));
    }

    [Test]
    public async Task GetNamespaceAsync_NotProvisioned_ReturnsNull()
    {
        var result = await _provisioner.GetNamespaceAsync("unknown");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeprovisionNamespaceAsync_Existing_ReturnsTrue()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("tenant-1", TenantPlan.Standard);
        await _provisioner.ProvisionNamespaceAsync("tenant-1", config);

        var removed = await _provisioner.DeprovisionNamespaceAsync("tenant-1");

        Assert.That(removed, Is.True);
    }

    [Test]
    public async Task DeprovisionNamespaceAsync_NotExisting_ReturnsFalse()
    {
        var removed = await _provisioner.DeprovisionNamespaceAsync("unknown");

        Assert.That(removed, Is.False);
    }

    [Test]
    public async Task DeprovisionNamespaceAsync_RemovesFromStore()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("tenant-1", TenantPlan.Standard);
        await _provisioner.ProvisionNamespaceAsync("tenant-1", config);

        await _provisioner.DeprovisionNamespaceAsync("tenant-1");

        var result = await _provisioner.GetNamespaceAsync("tenant-1");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void BuildDefaultConfig_StandardPlan_SharedIsolation()
    {
        var config = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig("Tenant X", TenantPlan.Standard);

        Assert.That(config.IsolationLevel, Is.EqualTo(IsolationLevel.Shared));
        Assert.That(config.NamespacePrefix, Is.EqualTo("ns-tenant-x"));
        Assert.That(config.QueuePrefix, Is.EqualTo("q-tenant-x"));
        Assert.That(config.TopicPrefix, Is.EqualTo("t-tenant-x"));
    }
}
