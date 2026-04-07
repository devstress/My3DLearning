// ============================================================================
// Tutorial 43 – Kubernetes Deployment / Configuration Options (Lab)
// ============================================================================
// EIP Pattern: Environment Cascade + Configuration Resolution.
// E2E: EnvironmentOverrideProvider backed by InMemoryConfigurationStore —
//      resolve config per environment, fall back to default, publish
//      resolved values to MockEndpoint.
// ============================================================================
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial43;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;
    private ConfigurationChangeNotifier _notifier = null!;

    [SetUp]
    public void SetUp()
    {
        _output = new MockEndpoint("deploy-out");
        _notifier = new ConfigurationChangeNotifier();
    }

    [TearDown]
    public async Task TearDown()
    {
        _notifier.Dispose();
        await _output.DisposeAsync();
    }

    private InMemoryConfigurationStore CreateStore() => new(_notifier);


    // ── 1. Environment Resolution ────────────────────────────────────

    [Test]
    public async Task EnvironmentOverride_ResolvesSpecificEnvironment()
    {
        var store = CreateStore();
        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "default"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "prod-db.internal", "prod"));

        var provider = new EnvironmentOverrideProvider(store);
        var resolved = await provider.ResolveAsync("Database:Host", "prod");

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Value, Is.EqualTo("prod-db.internal"));

        var envelope = IntegrationEnvelope<string>.Create(
            resolved.Value, "config-resolver", "config.resolved");
        await _output.PublishAsync(envelope, "resolved-config", default);
        _output.AssertReceivedOnTopic("resolved-config", 1);
    }

    [Test]
    public async Task EnvironmentOverride_FallsBackToDefault()
    {
        var store = CreateStore();
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300", "default"));

        var provider = new EnvironmentOverrideProvider(store);
        var resolved = await provider.ResolveAsync("Cache:Ttl", "staging");

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Value, Is.EqualTo("300"));
        Assert.That(resolved.Environment, Is.EqualTo("default"));

        var envelope = IntegrationEnvelope<string>.Create(
            resolved.Value, "config-resolver", "config.fallback");
        await _output.PublishAsync(envelope, "fallback-config", default);
        _output.AssertReceivedOnTopic("fallback-config", 1);
    }


    // ── 2. Batch Resolution ──────────────────────────────────────────

    [Test]
    public async Task EnvironmentOverride_ReturnsNull_WhenNotFound()
    {
        var store = CreateStore();
        var provider = new EnvironmentOverrideProvider(store);

        var resolved = await provider.ResolveAsync("Missing:Key", "dev");
        Assert.That(resolved, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            "not-found", "config-resolver", "config.missing");
        await _output.PublishAsync(envelope, "missing-config", default);
        _output.AssertReceivedOnTopic("missing-config", 1);
    }

    [Test]
    public async Task EnvironmentOverride_ResolveMany_PublishResults()
    {
        var store = CreateStore();
        await store.SetAsync(new ConfigurationEntry("App:Name", "MyApp", "default"));
        await store.SetAsync(new ConfigurationEntry("App:Version", "2.0", "prod"));

        var provider = new EnvironmentOverrideProvider(store);
        var resolved = await provider.ResolveManyAsync(
            new[] { "App:Name", "App:Version" }, "prod");

        Assert.That(resolved, Has.Count.EqualTo(2));
        Assert.That(resolved["App:Name"].Value, Is.EqualTo("MyApp"));
        Assert.That(resolved["App:Version"].Value, Is.EqualTo("2.0"));

        foreach (var kvp in resolved)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{kvp.Key}={kvp.Value.Value}", "config-resolver", "config.batch");
            await _output.PublishAsync(envelope, "batch-config", default);
        }

        _output.AssertReceivedOnTopic("batch-config", 2);
    }


    // ── 3. Variable Fallback ─────────────────────────────────────────

    [Test]
    public async Task ConfigCascade_DevStagingProd_PublishResolved()
    {
        var store = CreateStore();
        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://localhost:4222", "default"));
        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://staging:4222", "staging"));
        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://prod:4222", "prod"));

        var provider = new EnvironmentOverrideProvider(store);

        var environments = new[] { "dev", "staging", "prod" };
        foreach (var env in environments)
        {
            var resolved = await provider.ResolveAsync("Broker:Url", env);
            Assert.That(resolved, Is.Not.Null);

            var envelope = IntegrationEnvelope<string>.Create(
                resolved!.Value, "config-resolver", "config.cascade");
            await _output.PublishAsync(envelope, $"deploy-{env}", default);
        }

        // dev falls back to default
        var devMsg = _output.GetAllReceived<string>("deploy-dev");
        Assert.That(devMsg[0].Payload, Is.EqualTo("nats://localhost:4222"));

        _output.AssertReceivedOnTopic("deploy-staging", 1);
        _output.AssertReceivedOnTopic("deploy-prod", 1);
    }

    [Test]
    public async Task EnvironmentVariable_ResolveFromEnvVar()
    {
        var resolved = EnvironmentOverrideProvider.ResolveFromEnvironmentVariable("NonExistent:Key");
        Assert.That(resolved, Is.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            "env-var-check", "config-resolver", "config.envvar");
        await _output.PublishAsync(envelope, "envvar-results", default);
        _output.AssertReceivedOnTopic("envvar-results", 1);
    }
}
