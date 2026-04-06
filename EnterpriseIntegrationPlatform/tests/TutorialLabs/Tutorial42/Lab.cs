// ============================================================================
// Tutorial 42 – Configuration (Lab)
// ============================================================================
// EIP Pattern: Configuration Store + Feature Flags.
// E2E: InMemoryConfigurationStore + InMemoryFeatureFlagService +
//      MockEndpoint for config-driven routing decisions.
// ============================================================================
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;
    private ConfigurationChangeNotifier _notifier = null!;

    [SetUp]
    public void SetUp()
    {
        _output = new MockEndpoint("config-out");
        _notifier = new ConfigurationChangeNotifier();
    }

    [TearDown]
    public async Task TearDown()
    {
        _notifier.Dispose();
        await _output.DisposeAsync();
    }

    [Test]
    public async Task SetAndGet_PublishConfigValueToMockEndpoint()
    {
        var store = new InMemoryConfigurationStore(_notifier);

        var stored = await store.SetAsync(new ConfigurationEntry("App:Name", "MyApp"));
        var retrieved = await store.GetAsync("App:Name");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("MyApp"));
        Assert.That(stored.Version, Is.EqualTo(1));

        var envelope = IntegrationEnvelope<string>.Create(
            retrieved.Value, "config-store", "config.resolved");
        await _output.PublishAsync(envelope, "config-values", default);
        _output.AssertReceivedOnTopic("config-values", 1);
    }

    [Test]
    public async Task UpdateConfig_VersionIncrements_PublishChange()
    {
        var store = new InMemoryConfigurationStore(_notifier);

        var v1 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300"));
        Assert.That(v1.Version, Is.EqualTo(1));

        var v2 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "600"));
        Assert.That(v2.Version, Is.EqualTo(2));
        Assert.That(v2.Value, Is.EqualTo("600"));

        var envelope = IntegrationEnvelope<string>.Create(
            $"v{v2.Version}:{v2.Value}", "config-store", "config.updated");
        await _output.PublishAsync(envelope, "config-changes", default);
        _output.AssertReceivedOnTopic("config-changes", 1);
    }

    [Test]
    public async Task DeleteConfig_PublishDeletionNotification()
    {
        var store = new InMemoryConfigurationStore(_notifier);

        await store.SetAsync(new ConfigurationEntry("Temp:Key", "value"));
        var deleted = await store.DeleteAsync("Temp:Key");
        Assert.That(deleted, Is.True);

        var retrieved = await store.GetAsync("Temp:Key");
        Assert.That(retrieved, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            "Temp:Key", "config-store", "config.deleted");
        await _output.PublishAsync(envelope, "config-deletions", default);
        _output.AssertReceivedOnTopic("config-deletions", 1);
    }

    [Test]
    public async Task ListByEnvironment_PublishFilteredEntries()
    {
        var store = new InMemoryConfigurationStore(_notifier);

        await store.SetAsync(new ConfigurationEntry("Key1", "Val1", "dev"));
        await store.SetAsync(new ConfigurationEntry("Key2", "Val2", "dev"));
        await store.SetAsync(new ConfigurationEntry("Key3", "Val3", "prod"));

        var devEntries = await store.ListAsync("dev");
        Assert.That(devEntries, Has.Count.EqualTo(2));

        foreach (var entry in devEntries)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                entry.Value, "config-store", "config.listed");
            await _output.PublishAsync(envelope, "dev-config", default);
        }

        _output.AssertReceivedOnTopic("dev-config", 2);
    }

    [Test]
    public async Task FeatureFlag_SetAndEvaluate_PublishDecision()
    {
        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag("DarkMode", IsEnabled: true));
        var enabled = await service.IsEnabledAsync("DarkMode");
        Assert.That(enabled, Is.True);

        var topic = enabled ? "feature-enabled" : "feature-disabled";
        var envelope = IntegrationEnvelope<string>.Create(
            "DarkMode", "feature-flags", "flag.evaluated");
        await _output.PublishAsync(envelope, topic, default);
        _output.AssertReceivedOnTopic("feature-enabled", 1);
    }

    [Test]
    public async Task FeatureFlag_TargetTenant_PublishRouting()
    {
        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag(
            "BetaFeature", IsEnabled: true, RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant" }));

        var premiumEnabled = await service.IsEnabledAsync("BetaFeature", "premium-tenant");
        var regularEnabled = await service.IsEnabledAsync("BetaFeature", "regular-tenant");
        Assert.That(premiumEnabled, Is.True);
        Assert.That(regularEnabled, Is.False);

        var topic = premiumEnabled ? "beta-access" : "standard-access";
        var envelope = IntegrationEnvelope<string>.Create(
            "premium-tenant", "feature-flags", "flag.tenant");
        await _output.PublishAsync(envelope, topic, default);
        _output.AssertReceivedOnTopic("beta-access", 1);
    }

    [Test]
    public async Task FeatureFlag_GetVariant_PublishVariantValue()
    {
        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag(
            "ThemeSelector", IsEnabled: true,
            Variants: new Dictionary<string, string>
            {
                ["color"] = "blue",
                ["layout"] = "grid",
            }));

        var color = await service.GetVariantAsync("ThemeSelector", "color");
        var missing = await service.GetVariantAsync("ThemeSelector", "nonexistent");
        Assert.That(color, Is.EqualTo("blue"));
        Assert.That(missing, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            color!, "feature-flags", "flag.variant");
        await _output.PublishAsync(envelope, "variant-results", default);
        _output.AssertReceivedOnTopic("variant-results", 1);
    }
}
