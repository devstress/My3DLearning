// ============================================================================
// Tutorial 43 – Kubernetes Deployment (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full config cascade_ with nats broker endpoint
//   🟡 Intermediate  — multi key resolution_ across environments
//   🔴 Advanced      — deployment config scenario_ publish all resolved
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial43;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullConfigCascade_WithNatsBrokerEndpoint()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-cascade");
        var topic = AspireFixture.UniqueTopic("t43-exam-cascade-results");

        // TODO: Create a InMemoryConfigurationStore with appropriate configuration
        dynamic store = null!;

        // Set up cascade: default → environment-specific
        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "default"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Port", "5432", "default"));

        // TODO: Create a EnvironmentOverrideProvider with appropriate configuration
        dynamic provider = null!;

        // Staging gets specific host, default port
        // TODO: var host = await provider.ResolveAsync(...)
        dynamic host = null!;
        // TODO: var port = await provider.ResolveAsync(...)
        dynamic port = null!;
        Assert.That(host!.Value, Is.EqualTo("staging-db.internal"));
        Assert.That(port!.Value, Is.EqualTo("5432"));

        // Dev falls back to default for both
        // TODO: var devHost = await provider.ResolveAsync(...)
        dynamic devHost = null!;
        Assert.That(devHost!.Value, Is.EqualTo("localhost"));

        var results = new[] { host, port, devHost };
        foreach (var entry in results)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Intermediate_MultiKeyResolution_AcrossEnvironments()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-multikey");
        var topic = AspireFixture.UniqueTopic("t43-exam-prod-config");

        // TODO: Create a InMemoryConfigurationStore with appropriate configuration
        dynamic store = null!;

        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://localhost:4222", "default"));
        await store.SetAsync(new ConfigurationEntry("Broker:Url", "nats://prod:4222", "prod"));
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "60", "default"));
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300", "prod"));
        await store.SetAsync(new ConfigurationEntry("App:Version", "1.0.0", "default"));

        // TODO: Create a EnvironmentOverrideProvider with appropriate configuration
        dynamic provider = null!;
        var keys = new[] { "Broker:Url", "Cache:Ttl", "App:Version" };

        // TODO: var prodConfig = await provider.ResolveManyAsync(...)
        dynamic prodConfig = null!;
        Assert.That(prodConfig, Has.Count.EqualTo(3));
        Assert.That(prodConfig["Broker:Url"].Value, Is.EqualTo("nats://prod:4222"));
        Assert.That(prodConfig["Cache:Ttl"].Value, Is.EqualTo("300"));
        Assert.That(prodConfig["App:Version"].Value, Is.EqualTo("1.0.0"));

        foreach (var kvp in prodConfig)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Advanced_DeploymentConfigScenario_PublishAllResolved()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t43-exam-deploy");
        var topic = AspireFixture.UniqueTopic("t43-exam-deploy-manifest");

        // TODO: Create a InMemoryConfigurationStore with appropriate configuration
        dynamic store = null!;

        // Simulate K8s-style config: defaults + per-namespace overrides
        await store.SetAsync(new ConfigurationEntry("Replicas", "1", "default"));
        await store.SetAsync(new ConfigurationEntry("Replicas", "3", "prod"));
        await store.SetAsync(new ConfigurationEntry("Memory:Limit", "512Mi", "default"));
        await store.SetAsync(new ConfigurationEntry("Memory:Limit", "2Gi", "prod"));
        await store.SetAsync(new ConfigurationEntry("Log:Level", "Debug", "default"));
        await store.SetAsync(new ConfigurationEntry("Log:Level", "Warning", "prod"));

        // TODO: Create a EnvironmentOverrideProvider with appropriate configuration
        dynamic provider = null!;
        var keys = new[] { "Replicas", "Memory:Limit", "Log:Level" };

        // Compare default vs prod
        // TODO: var defaultConfig = await provider.ResolveManyAsync(...)
        dynamic defaultConfig = null!;
        // TODO: var prodConfig = await provider.ResolveManyAsync(...)
        dynamic prodConfig = null!;

        Assert.That(defaultConfig["Replicas"].Value, Is.EqualTo("1"));
        Assert.That(prodConfig["Replicas"].Value, Is.EqualTo("3"));
        Assert.That(prodConfig["Log:Level"].Value, Is.EqualTo("Warning"));

        foreach (var kvp in prodConfig)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }
}
#endif
