// ============================================================================
// Tutorial 42 – Configuration (Exam)
// ============================================================================
// Coding challenges: multi-environment config management, feature flag
// tenant targeting, and configuration versioning.
// ============================================================================

using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Multi-Environment Configuration Management ─────────────

    [Test]
    public async Task Challenge1_MultiEnvironment_ConfigurationManagement()
    {
        using var notifier = new ConfigurationChangeNotifier();
        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "dev"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "prod-db.internal", "prod"));

        var devHost = await store.GetAsync("Database:Host", "dev");
        var stagingHost = await store.GetAsync("Database:Host", "staging");
        var prodHost = await store.GetAsync("Database:Host", "prod");

        Assert.That(devHost, Is.Not.Null);
        Assert.That(devHost!.Value, Is.EqualTo("localhost"));
        Assert.That(stagingHost!.Value, Is.EqualTo("staging-db.internal"));
        Assert.That(prodHost!.Value, Is.EqualTo("prod-db.internal"));

        // Each environment is independent — delete dev, others remain
        await store.DeleteAsync("Database:Host", "dev");
        Assert.That(await store.GetAsync("Database:Host", "dev"), Is.Null);
        Assert.That((await store.GetAsync("Database:Host", "staging"))!.Value,
            Is.EqualTo("staging-db.internal"));
        Assert.That((await store.GetAsync("Database:Host", "prod"))!.Value,
            Is.EqualTo("prod-db.internal"));
    }

    // ── Challenge 2: Feature Flag with Tenant Targeting ─────────────────────

    [Test]
    public async Task Challenge2_FeatureFlag_WithTenantTargeting()
    {
        var service = new InMemoryFeatureFlagService();

        var flag = new FeatureFlag(
            "BetaFeature",
            IsEnabled: true,
            RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant", "early-adopter" });

        await service.SetAsync(flag);

        // Targeted tenants get the feature despite 0% rollout
        var premiumEnabled = await service.IsEnabledAsync("BetaFeature", "premium-tenant");
        var earlyAdopterEnabled = await service.IsEnabledAsync("BetaFeature", "early-adopter");
        Assert.That(premiumEnabled, Is.True);
        Assert.That(earlyAdopterEnabled, Is.True);

        // Non-targeted tenants do not get the feature at 0% rollout
        var regularEnabled = await service.IsEnabledAsync("BetaFeature", "regular-tenant");
        Assert.That(regularEnabled, Is.False);
    }

    // ── Challenge 3: Configuration Versioning ───────────────────────────────

    [Test]
    public async Task Challenge3_ConfigurationVersioning_IncrementOnUpdate()
    {
        using var notifier = new ConfigurationChangeNotifier();
        var store = new InMemoryConfigurationStore(notifier);

        var entry1 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300"));
        Assert.That(entry1.Version, Is.EqualTo(1));

        var entry2 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "600"));
        Assert.That(entry2.Version, Is.EqualTo(2));

        var entry3 = await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "900"));
        Assert.That(entry3.Version, Is.EqualTo(3));

        var retrieved = await store.GetAsync("Cache:Ttl");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("900"));
        Assert.That(retrieved.Version, Is.EqualTo(3));
    }
}
