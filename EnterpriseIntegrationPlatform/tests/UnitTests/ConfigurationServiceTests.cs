using EnterpriseIntegrationPlatform.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class ConfigurationServiceTests
{
    private InMemoryFeatureFlagService _flagService = null!;
    private IConfigurationStore _mockStore = null!;
    private EnvironmentOverrideProvider _overrideProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _flagService = new InMemoryFeatureFlagService();
        _mockStore = Substitute.For<IConfigurationStore>();
        _overrideProvider = new EnvironmentOverrideProvider(_mockStore);
    }

    // ── InMemoryFeatureFlagService ──────────────────────────────────────

    [Test]
    public async Task IsEnabledAsync_FlagNotFound_ReturnsFalse()
    {
        var result = await _flagService.IsEnabledAsync("nonexistent");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_FlagDisabled_ReturnsFalse()
    {
        await _flagService.SetAsync(new FeatureFlag("dark-mode", IsEnabled: false));

        var result = await _flagService.IsEnabledAsync("dark-mode");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_EnabledFullRollout_ReturnsTrue()
    {
        await _flagService.SetAsync(new FeatureFlag("new-ui", IsEnabled: true, RolloutPercentage: 100));

        var result = await _flagService.IsEnabledAsync("new-ui");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsEnabledAsync_EnabledZeroRolloutNoTarget_ReturnsFalse()
    {
        await _flagService.SetAsync(new FeatureFlag("beta", IsEnabled: true, RolloutPercentage: 0));

        var result = await _flagService.IsEnabledAsync("beta", "some-tenant");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_TargetedTenant_ReturnsTrueRegardlessOfRollout()
    {
        await _flagService.SetAsync(new FeatureFlag(
            "premium",
            IsEnabled: true,
            RolloutPercentage: 0,
            TargetTenants: ["vip-corp"]));

        var result = await _flagService.IsEnabledAsync("premium", "vip-corp");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetVariantAsync_FlagAndVariantExist_ReturnsValue()
    {
        await _flagService.SetAsync(new FeatureFlag(
            "experiment",
            IsEnabled: true,
            Variants: new Dictionary<string, string> { ["color"] = "blue" }));

        var value = await _flagService.GetVariantAsync("experiment", "color");

        Assert.That(value, Is.EqualTo("blue"));
    }

    [Test]
    public async Task GetVariantAsync_FlagNotFound_ReturnsNull()
    {
        var value = await _flagService.GetVariantAsync("ghost", "any-key");

        Assert.That(value, Is.Null);
    }

    [Test]
    public async Task SetGetDeleteList_CrudLifecycle_WorksCorrectly()
    {
        var flag = new FeatureFlag("lifecycle-flag", IsEnabled: true, RolloutPercentage: 50);

        await _flagService.SetAsync(flag);
        var fetched = await _flagService.GetAsync("lifecycle-flag");
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.RolloutPercentage, Is.EqualTo(50));

        var list = await _flagService.ListAsync();
        Assert.That(list, Has.Count.EqualTo(1));

        var deleted = await _flagService.DeleteAsync("lifecycle-flag");
        Assert.That(deleted, Is.True);

        var afterDelete = await _flagService.GetAsync("lifecycle-flag");
        Assert.That(afterDelete, Is.Null);
    }

    [Test]
    public void IsEnabledAsync_NullName_ThrowsArgumentException()
    {
        Assert.That(async () => await _flagService.IsEnabledAsync(null!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task GetAsync_CaseInsensitiveLookup_ReturnsFlag()
    {
        await _flagService.SetAsync(new FeatureFlag("CaseSensitive", IsEnabled: true));

        var result = await _flagService.GetAsync("casesensitive");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("CaseSensitive"));
    }

    // ── EnvironmentOverrideProvider (with NSubstitute mock) ─────────────

    [Test]
    public async Task ResolveAsync_SpecificEnvPresent_ReturnsSpecificEntry()
    {
        var expected = new ConfigurationEntry("cache:ttl", "300", "prod");
        _mockStore.GetAsync("cache:ttl", "prod", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _overrideProvider.ResolveAsync("cache:ttl", "prod");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("300"));
        Assert.That(result.Environment, Is.EqualTo("prod"));
    }

    [Test]
    public async Task ResolveAsync_SpecificEnvMissing_FallsBackToDefault()
    {
        var defaultEntry = new ConfigurationEntry("cache:ttl", "60", "default");
        _mockStore.GetAsync("cache:ttl", "staging", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);
        _mockStore.GetAsync("cache:ttl", "default", Arg.Any<CancellationToken>())
            .Returns(defaultEntry);

        var result = await _overrideProvider.ResolveAsync("cache:ttl", "staging");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("60"));
        Assert.That(result.Environment, Is.EqualTo("default"));
    }

    [Test]
    public async Task ResolveAsync_NeitherSpecificNorDefault_ReturnsNull()
    {
        _mockStore.GetAsync("missing:key", "prod", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);
        _mockStore.GetAsync("missing:key", "default", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);

        var result = await _overrideProvider.ResolveAsync("missing:key", "prod");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ResolveAsync_DefaultEnvironment_DoesNotQueryDefaultTwice()
    {
        var entry = new ConfigurationEntry("log:level", "Info", "default");
        _mockStore.GetAsync("log:level", "default", Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _overrideProvider.ResolveAsync("log:level", "default");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("Info"));
        // Store should be called exactly once — no redundant fallback query
        await _mockStore.Received(1).GetAsync("log:level", "default", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveManyAsync_MultipleKeys_ResolvesWithFallback()
    {
        var prodEntry = new ConfigurationEntry("db:host", "prod-db", "prod");
        var defaultTimeout = new ConfigurationEntry("api:timeout", "30", "default");

        _mockStore.GetAsync("db:host", "prod", Arg.Any<CancellationToken>())
            .Returns(prodEntry);
        _mockStore.GetAsync("api:timeout", "prod", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);
        _mockStore.GetAsync("api:timeout", "default", Arg.Any<CancellationToken>())
            .Returns(defaultTimeout);
        _mockStore.GetAsync("missing", "prod", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);
        _mockStore.GetAsync("missing", "default", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);

        var results = await _overrideProvider.ResolveManyAsync(
            ["db:host", "api:timeout", "missing"], "prod");

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results["db:host"].Value, Is.EqualTo("prod-db"));
        Assert.That(results["api:timeout"].Value, Is.EqualTo("30"));
        Assert.That(results.ContainsKey("missing"), Is.False);
    }
}
