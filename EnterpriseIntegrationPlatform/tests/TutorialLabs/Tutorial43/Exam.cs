// ============================================================================
// Tutorial 43 – Kubernetes Deployment / Configuration Options (Exam)
// ============================================================================
// EIP Pattern: Environment Cascade + Configuration Resolution
// E2E: Full config cascade across all levels, multi-key resolution, and
//      deployment-scenario configuration — all via NatsBrokerEndpoint
//      (real NATS JetStream via Aspire).
// ============================================================================
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial43;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullConfigCascade_WithNatsBrokerEndpoint()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-cascade");
        var topic = AspireFixture.UniqueTopic("t43-exam-cascade-results");

        var store = new InMemoryConfigurationStore(notifier);

        // Set up cascade: default → environment-specific
        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "default"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Port", "5432", "default"));

        var provider = new EnvironmentOverrideProvider(store);

        // Staging gets specific host, default port
        var host = await provider.ResolveAsync("Database:Host", "staging");
        var port = await provider.ResolveAsync("Database:Port", "staging");
        Assert.That(host!.Value, Is.EqualTo("staging-db.internal"));
        Assert.That(port!.Value, Is.EqualTo("5432"));

        // Dev falls back to default for both
        var devHost = await provider.ResolveAsync("Database:Host", "dev");
        Assert.That(devHost!.Value, Is.EqualTo("localhost"));

        var results = new[] { host, port, devHost };
        foreach (var entry in results)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{entry!.Key}={entry.Value}", "config-resolver", "config.cascade");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Challenge2_MultiKeyResolution_AcrossEnvironments()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-multikey");
        var topic = AspireFixture.UniqueTopic("t43-exam-prod-config");

        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://localhost:4222", "default"));
        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://prod:4222", "prod"));
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "60", "default"));
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300", "prod"));
        await store.SetAsync(new ConfigurationEntry("App:Version", "1.0.0", "default"));

        var provider = new EnvironmentOverrideProvider(store);
        var keys = new[] { "Broker:Url", "Cache:Ttl", "App:Version" };

        var prodConfig = await provider.ResolveManyAsync(keys, "prod");
        Assert.That(prodConfig, Has.Count.EqualTo(3));
        Assert.That(prodConfig["Broker:Url"].Value, Is.EqualTo("nats://prod:4222"));
        Assert.That(prodConfig["Cache:Ttl"].Value, Is.EqualTo("300"));
        Assert.That(prodConfig["App:Version"].Value, Is.EqualTo("1.0.0"));

        foreach (var kvp in prodConfig)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{kvp.Key}={kvp.Value.Value}", "config-resolver", "config.multi");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Challenge3_DeploymentConfigScenario_PublishAllResolved()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-deploy");
        var topic = AspireFixture.UniqueTopic("t43-exam-deploy-manifest");

        var store = new InMemoryConfigurationStore(notifier);

        // Simulate K8s-style config: defaults + per-namespace overrides
        await store.SetAsync(new ConfigurationEntry("Replicas", "1", "default"));
        await store.SetAsync(new ConfigurationEntry("Replicas", "3", "prod"));
        await store.SetAsync(new ConfigurationEntry("Memory:Limit", "512Mi", "default"));
        await store.SetAsync(new ConfigurationEntry("Memory:Limit", "2Gi", "prod"));
        await store.SetAsync(new ConfigurationEntry("Log:Level", "Debug", "default"));
        await store.SetAsync(new ConfigurationEntry("Log:Level", "Warning", "prod"));

        var provider = new EnvironmentOverrideProvider(store);
        var keys = new[] { "Replicas", "Memory:Limit", "Log:Level" };

        // Compare default vs prod
        var defaultConfig = await provider.ResolveManyAsync(keys, "dev");
        var prodConfig = await provider.ResolveManyAsync(keys, "prod");

        Assert.That(defaultConfig["Replicas"].Value, Is.EqualTo("1"));
        Assert.That(prodConfig["Replicas"].Value, Is.EqualTo("3"));
        Assert.That(prodConfig["Log:Level"].Value, Is.EqualTo("Warning"));

        foreach (var kvp in prodConfig)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                kvp.Value.Value, "deployer", "deploy.config");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }
}
