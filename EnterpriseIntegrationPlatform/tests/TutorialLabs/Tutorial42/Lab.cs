// ============================================================================
// Tutorial 42 – Configuration (Lab)
// ============================================================================
// This lab exercises InMemoryConfigurationStore, InMemoryFeatureFlagService,
// ConfigurationEntry, FeatureFlag, and ConfigurationChange records.
// ============================================================================

using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Lab
{
    // ── ConfigurationEntry Record Defaults ───────────────────────────────────

    [Test]
    public void ConfigurationEntry_Defaults_EnvironmentAndVersion()
    {
        var entry = new ConfigurationEntry("Database:Host", "localhost");

        Assert.That(entry.Key, Is.EqualTo("Database:Host"));
        Assert.That(entry.Value, Is.EqualTo("localhost"));
        Assert.That(entry.Environment, Is.EqualTo("default"));
        Assert.That(entry.Version, Is.EqualTo(1));
        Assert.That(entry.ModifiedBy, Is.Null);
        Assert.That(entry.LastModified, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    // ── InMemoryConfigurationStore: Set and Get Roundtrip ───────────────────

    [Test]
    public async Task InMemoryConfigurationStore_SetAndGet_Roundtrip()
    {
        using var notifier = new ConfigurationChangeNotifier();
        var store = new InMemoryConfigurationStore(notifier);

        var entry = new ConfigurationEntry("App:Name", "MyApp");
        await store.SetAsync(entry);

        var retrieved = await store.GetAsync("App:Name");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("MyApp"));
        Assert.That(retrieved.Version, Is.EqualTo(1));
    }

    // ── Set, Delete, Get Returns Null ───────────────────────────────────────

    [Test]
    public async Task InMemoryConfigurationStore_SetDeleteGet_ReturnsNull()
    {
        using var notifier = new ConfigurationChangeNotifier();
        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Temp:Key", "value"));
        var deleted = await store.DeleteAsync("Temp:Key");
        var retrieved = await store.GetAsync("Temp:Key");

        Assert.That(deleted, Is.True);
        Assert.That(retrieved, Is.Null);
    }

    // ── List Returns All Entries ─────────────────────────────────────────────

    [Test]
    public async Task InMemoryConfigurationStore_List_ReturnsAllEntries()
    {
        using var notifier = new ConfigurationChangeNotifier();
        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Key1", "Val1", "dev"));
        await store.SetAsync(new ConfigurationEntry("Key2", "Val2", "dev"));
        await store.SetAsync(new ConfigurationEntry("Key3", "Val3", "prod"));

        var allEntries = await store.ListAsync();
        Assert.That(allEntries, Has.Count.EqualTo(3));

        var devEntries = await store.ListAsync("dev");
        Assert.That(devEntries, Has.Count.EqualTo(2));
    }

    // ── FeatureFlag Record Shape ────────────────────────────────────────────

    [Test]
    public void FeatureFlag_RecordShape()
    {
        var flag = new FeatureFlag(
            Name: "NewCheckout",
            IsEnabled: true,
            Variants: new Dictionary<string, string> { ["control"] = "v1", ["treatment"] = "v2" },
            RolloutPercentage: 50,
            TargetTenants: new List<string> { "tenant-a", "tenant-b" });

        Assert.That(flag.Name, Is.EqualTo("NewCheckout"));
        Assert.That(flag.IsEnabled, Is.True);
        Assert.That(flag.Variants, Has.Count.EqualTo(2));
        Assert.That(flag.RolloutPercentage, Is.EqualTo(50));
        Assert.That(flag.TargetTenants, Has.Count.EqualTo(2));
    }

    // ── InMemoryFeatureFlagService: Set and Check IsEnabledAsync ────────────

    [Test]
    public async Task InMemoryFeatureFlagService_SetAndCheck_IsEnabledAsync()
    {
        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag("DarkMode", IsEnabled: true));

        var enabled = await service.IsEnabledAsync("DarkMode");
        Assert.That(enabled, Is.True);

        await service.SetAsync(new FeatureFlag("DarkMode", IsEnabled: false));
        var disabled = await service.IsEnabledAsync("DarkMode");
        Assert.That(disabled, Is.False);
    }

    // ── InMemoryFeatureFlagService: Set Variant and GetVariantAsync ─────────

    [Test]
    public async Task InMemoryFeatureFlagService_SetVariant_GetVariantAsync()
    {
        var service = new InMemoryFeatureFlagService();

        var flag = new FeatureFlag(
            "ThemeSelector",
            IsEnabled: true,
            Variants: new Dictionary<string, string>
            {
                ["color"] = "blue",
                ["layout"] = "grid",
            });

        await service.SetAsync(flag);

        var color = await service.GetVariantAsync("ThemeSelector", "color");
        var layout = await service.GetVariantAsync("ThemeSelector", "layout");
        var missing = await service.GetVariantAsync("ThemeSelector", "nonexistent");

        Assert.That(color, Is.EqualTo("blue"));
        Assert.That(layout, Is.EqualTo("grid"));
        Assert.That(missing, Is.Null);
    }
}
