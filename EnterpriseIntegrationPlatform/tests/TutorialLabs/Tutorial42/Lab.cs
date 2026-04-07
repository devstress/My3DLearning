// ============================================================================
// Tutorial 42 – Configuration (Lab)
// ============================================================================
// EIP Pattern: Configuration Store + Feature Flags.
// E2E: InMemoryConfigurationStore + InMemoryFeatureFlagService +
//      NatsBrokerEndpoint (real NATS JetStream via Aspire) for
//      config-driven routing decisions.
// ============================================================================
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Lab
{
    private ConfigurationChangeNotifier _notifier = null!;

    [SetUp]
    public void SetUp()
    {
        _notifier = new ConfigurationChangeNotifier();
    }

    [TearDown]
    public void TearDown()
    {
        _notifier.Dispose();
    }


    // ── 1. Configuration Store CRUD ──────────────────────────────────

    [Test]
    public async Task SetAndGet_PublishConfigValueToNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-setget");
        var topic = AspireFixture.UniqueTopic("t42-config-values");

        var store = new InMemoryConfigurationStore(_notifier);

        var stored = await store.SetAsync(new ConfigurationEntry("App:Name", "MyApp"));
        var retrieved = await store.GetAsync("App:Name");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("MyApp"));
        Assert.That(stored.Version, Is.EqualTo(1));

        var envelope = IntegrationEnvelope<string>.Create(
            retrieved.Value, "config-store", "config.resolved");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task UpdateConfig_VersionIncrements_PublishChange()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-update");
        var topic = AspireFixture.UniqueTopic("t42-config-changes");

        var store = new InMemoryConfigurationStore(_notifier);

        var v1 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300"));
        Assert.That(v1.Version, Is.EqualTo(1));

        var v2 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "600"));
        Assert.That(v2.Version, Is.EqualTo(2));
        Assert.That(v2.Value, Is.EqualTo("600"));

        var envelope = IntegrationEnvelope<string>.Create(
            $"v{v2.Version}:{v2.Value}", "config-store", "config.updated");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Environment-Scoped Configuration ──────────────────────────

    [Test]
    public async Task DeleteConfig_PublishDeletionNotification()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-delete");
        var topic = AspireFixture.UniqueTopic("t42-config-deletions");

        var store = new InMemoryConfigurationStore(_notifier);

        await store.SetAsync(new ConfigurationEntry("Temp:Key", "value"));
        var deleted = await store.DeleteAsync("Temp:Key");
        Assert.That(deleted, Is.True);

        var retrieved = await store.GetAsync("Temp:Key");
        Assert.That(retrieved, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            "Temp:Key", "config-store", "config.deleted");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task ListByEnvironment_PublishFilteredEntries()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-listenv");
        var topic = AspireFixture.UniqueTopic("t42-dev-config");

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
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 2);
    }

    [Test]
    public async Task FeatureFlag_SetAndEvaluate_PublishDecision()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-flag-eval");
        var enabledTopic = AspireFixture.UniqueTopic("t42-feature-enabled");
        var disabledTopic = AspireFixture.UniqueTopic("t42-feature-disabled");

        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag("DarkMode", IsEnabled: true));
        var enabled = await service.IsEnabledAsync("DarkMode");
        Assert.That(enabled, Is.True);

        var topic = enabled ? enabledTopic : disabledTopic;
        var envelope = IntegrationEnvelope<string>.Create(
            "DarkMode", "feature-flags", "flag.evaluated");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(enabledTopic, 1);
    }


    // ── 3. Feature Flag Evaluation ───────────────────────────────────

    [Test]
    public async Task FeatureFlag_TargetTenant_PublishRouting()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-flag-tenant");
        var betaTopic = AspireFixture.UniqueTopic("t42-beta-access");
        var standardTopic = AspireFixture.UniqueTopic("t42-standard-access");

        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag(
            "BetaFeature", IsEnabled: true, RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant" }));

        var premiumEnabled = await service.IsEnabledAsync("BetaFeature", "premium-tenant");
        var regularEnabled = await service.IsEnabledAsync("BetaFeature", "regular-tenant");
        Assert.That(premiumEnabled, Is.True);
        Assert.That(regularEnabled, Is.False);

        var topic = premiumEnabled ? betaTopic : standardTopic;
        var envelope = IntegrationEnvelope<string>.Create(
            "premium-tenant", "feature-flags", "flag.tenant");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(betaTopic, 1);
    }

    [Test]
    public async Task FeatureFlag_GetVariant_PublishVariantValue()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-flag-variant");
        var topic = AspireFixture.UniqueTopic("t42-variant-results");

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
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }
}
