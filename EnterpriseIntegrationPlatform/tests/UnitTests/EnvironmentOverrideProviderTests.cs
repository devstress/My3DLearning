using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class EnvironmentOverrideProviderTests
{
    private ConfigurationChangeNotifier _notifier = null!;
    private InMemoryConfigurationStore _store = null!;
    private EnvironmentOverrideProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _notifier = new ConfigurationChangeNotifier();
        _store = new InMemoryConfigurationStore(_notifier);
        _provider = new EnvironmentOverrideProvider(_store);
    }

    [TearDown]
    public void TearDown()
    {
        _notifier.Dispose();
    }

    [Test]
    public async Task ResolveAsync_SpecificEnvironmentExists_ReturnsSpecific()
    {
        await _store.SetAsync(new ConfigurationEntry("db:host", "default-host", "default"));
        await _store.SetAsync(new ConfigurationEntry("db:host", "prod-host", "prod"));

        var result = await _provider.ResolveAsync("db:host", "prod");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("prod-host"));
        Assert.That(result.Environment, Is.EqualTo("prod"));
    }

    [Test]
    public async Task ResolveAsync_SpecificNotFound_FallsBackToDefault()
    {
        await _store.SetAsync(new ConfigurationEntry("db:host", "default-host", "default"));

        var result = await _provider.ResolveAsync("db:host", "staging");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("default-host"));
        Assert.That(result.Environment, Is.EqualTo("default"));
    }

    [Test]
    public async Task ResolveAsync_NeitherSpecificNorDefault_ReturnsNull()
    {
        var result = await _provider.ResolveAsync("missing:key", "prod");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ResolveAsync_DefaultEnvironment_DoesNotDoubleQuery()
    {
        await _store.SetAsync(new ConfigurationEntry("db:host", "default-value", "default"));

        var result = await _provider.ResolveAsync("db:host", "default");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("default-value"));
    }

    [Test]
    public async Task ResolveAsync_DevEnvironment_FallsBackToDefault()
    {
        await _store.SetAsync(new ConfigurationEntry("api:timeout", "30s", "default"));

        var result = await _provider.ResolveAsync("api:timeout", "dev");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("30s"));
    }

    [Test]
    public async Task ResolveAsync_DevEnvironmentOverrides_ReturnsDevValue()
    {
        await _store.SetAsync(new ConfigurationEntry("api:timeout", "30s", "default"));
        await _store.SetAsync(new ConfigurationEntry("api:timeout", "5s", "dev"));

        var result = await _provider.ResolveAsync("api:timeout", "dev");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("5s"));
    }

    [Test]
    public async Task ResolveManyAsync_MixedExistence_ReturnsOnlyFound()
    {
        await _store.SetAsync(new ConfigurationEntry("key1", "value1", "default"));
        await _store.SetAsync(new ConfigurationEntry("key2", "value2", "prod"));

        var results = await _provider.ResolveManyAsync(["key1", "key2", "key3"], "prod");

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.ContainsKey("key1"), Is.True);
        Assert.That(results.ContainsKey("key2"), Is.True);
        Assert.That(results.ContainsKey("key3"), Is.False);
    }

    [Test]
    public async Task ResolveManyAsync_EmptyKeys_ReturnsEmpty()
    {
        var results = await _provider.ResolveManyAsync([], "prod");

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task ResolveAsync_CascadeWorksForAllStandardEnvironments()
    {
        await _store.SetAsync(new ConfigurationEntry("feature:x", "default-val", "default"));

        foreach (var env in new[] { "dev", "staging", "prod" })
        {
            var result = await _provider.ResolveAsync("feature:x", env);
            Assert.That(result, Is.Not.Null, $"Expected fallback for environment '{env}'");
            Assert.That(result!.Value, Is.EqualTo("default-val"));
        }
    }

    // ───── EIP__ Environment Variable Convention ─────

    [Test]
    public async Task ResolveAsync_EipEnvVar_OverridesStoreValue()
    {
        var envVar = "EIP__Broker__ConnectionString";
        try
        {
            Environment.SetEnvironmentVariable(envVar, "from-env");
            await _store.SetAsync(new ConfigurationEntry("Broker:ConnectionString", "from-store", "prod"));

            var result = await _provider.ResolveAsync("Broker:ConnectionString", "prod");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value, Is.EqualTo("from-env"));
            Assert.That(result.Environment, Is.EqualTo("environment-variable"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    [Test]
    public async Task ResolveAsync_EipEnvVar_NotSet_FallsToStore()
    {
        await _store.SetAsync(new ConfigurationEntry("Broker:ConnectionString", "from-store", "prod"));

        var result = await _provider.ResolveAsync("Broker:ConnectionString", "prod");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("from-store"));
    }

    [Test]
    public void ResolveFromEnvironmentVariable_MapsColonsToDoubleUnderscore()
    {
        var envVar = "EIP__Section__Nested__Key";
        try
        {
            Environment.SetEnvironmentVariable(envVar, "deep-value");

            var result = EnvironmentOverrideProvider.ResolveFromEnvironmentVariable("Section:Nested:Key");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value, Is.EqualTo("deep-value"));
            Assert.That(result.Key, Is.EqualTo("Section:Nested:Key"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    [Test]
    public void ResolveFromEnvironmentVariable_MissingVar_ReturnsNull()
    {
        var result = EnvironmentOverrideProvider.ResolveFromEnvironmentVariable("Missing:Key:Here");
        Assert.That(result, Is.Null);
    }
}
