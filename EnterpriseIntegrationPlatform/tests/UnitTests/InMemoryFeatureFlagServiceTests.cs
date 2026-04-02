using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class InMemoryFeatureFlagServiceTests
{
    private InMemoryFeatureFlagService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new InMemoryFeatureFlagService();
    }

    [Test]
    public async Task IsEnabledAsync_NonExistentFlag_ReturnsFalse()
    {
        var result = await _service.IsEnabledAsync("missing-flag");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_DisabledFlag_ReturnsFalse()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: false));

        var result = await _service.IsEnabledAsync("my-flag");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_EnabledFlag_FullRollout_ReturnsTrue()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true, RolloutPercentage: 100));

        var result = await _service.IsEnabledAsync("my-flag");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsEnabledAsync_EnabledFlag_ZeroRollout_ReturnsFalse()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true, RolloutPercentage: 0));

        var result = await _service.IsEnabledAsync("my-flag");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_TargetTenant_AlwaysEnabled()
    {
        await _service.SetAsync(new FeatureFlag(
            "my-flag",
            IsEnabled: true,
            RolloutPercentage: 0,
            TargetTenants: ["acme", "contoso"]));

        var result = await _service.IsEnabledAsync("my-flag", "acme");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsEnabledAsync_NonTargetTenant_ZeroRollout_ReturnsFalse()
    {
        await _service.SetAsync(new FeatureFlag(
            "my-flag",
            IsEnabled: true,
            RolloutPercentage: 0,
            TargetTenants: ["acme"]));

        var result = await _service.IsEnabledAsync("my-flag", "other-tenant");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsEnabledAsync_PartialRollout_DeterministicForSameTenant()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true, RolloutPercentage: 50));

        var result1 = await _service.IsEnabledAsync("my-flag", "tenant-a");
        var result2 = await _service.IsEnabledAsync("my-flag", "tenant-a");

        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public async Task GetVariantAsync_ExistingVariant_ReturnsValue()
    {
        await _service.SetAsync(new FeatureFlag(
            "checkout-flow",
            IsEnabled: true,
            Variants: new Dictionary<string, string>
            {
                ["control"] = "v1",
                ["treatment"] = "v2"
            }));

        var variant = await _service.GetVariantAsync("checkout-flow", "treatment");

        Assert.That(variant, Is.EqualTo("v2"));
    }

    [Test]
    public async Task GetVariantAsync_NonExistentVariant_ReturnsNull()
    {
        await _service.SetAsync(new FeatureFlag(
            "checkout-flow",
            IsEnabled: true,
            Variants: new Dictionary<string, string> { ["control"] = "v1" }));

        var variant = await _service.GetVariantAsync("checkout-flow", "nonexistent");

        Assert.That(variant, Is.Null);
    }

    [Test]
    public async Task GetVariantAsync_NonExistentFlag_ReturnsNull()
    {
        var variant = await _service.GetVariantAsync("missing", "key");

        Assert.That(variant, Is.Null);
    }

    [Test]
    public async Task GetAsync_ExistingFlag_ReturnsFlag()
    {
        var flag = new FeatureFlag("my-flag", IsEnabled: true);
        await _service.SetAsync(flag);

        var result = await _service.GetAsync("my-flag");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("my-flag"));
        Assert.That(result.IsEnabled, Is.True);
    }

    [Test]
    public async Task GetAsync_NonExistentFlag_ReturnsNull()
    {
        var result = await _service.GetAsync("missing-flag");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_UpdatesExistingFlag()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: false));
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true));

        var result = await _service.GetAsync("my-flag");

        Assert.That(result!.IsEnabled, Is.True);
    }

    [Test]
    public async Task DeleteAsync_ExistingFlag_ReturnsTrue()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true));

        var deleted = await _service.DeleteAsync("my-flag");

        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task DeleteAsync_NonExistentFlag_ReturnsFalse()
    {
        var deleted = await _service.DeleteAsync("missing");

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task DeleteAsync_AfterDelete_GetReturnsNull()
    {
        await _service.SetAsync(new FeatureFlag("my-flag", IsEnabled: true));
        await _service.DeleteAsync("my-flag");

        var result = await _service.GetAsync("my-flag");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ListAsync_ReturnsAllFlags()
    {
        await _service.SetAsync(new FeatureFlag("flag1", IsEnabled: true));
        await _service.SetAsync(new FeatureFlag("flag2", IsEnabled: false));
        await _service.SetAsync(new FeatureFlag("flag3", IsEnabled: true));

        var all = await _service.ListAsync();

        Assert.That(all, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task IsEnabledAsync_CaseInsensitiveLookup_FindsFlag()
    {
        await _service.SetAsync(new FeatureFlag("MyFlag", IsEnabled: true));

        var result = await _service.IsEnabledAsync("myflag");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SetAsync_ConcurrentAccess_MaintainsConsistency()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _service.SetAsync(new FeatureFlag($"flag{i}", IsEnabled: true)));

        await Task.WhenAll(tasks);

        var all = await _service.ListAsync();
        Assert.That(all, Has.Count.EqualTo(100));
    }
}
