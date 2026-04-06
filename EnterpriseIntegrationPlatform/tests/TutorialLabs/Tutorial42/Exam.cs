// ============================================================================
// Tutorial 42 – Configuration (Exam)
// ============================================================================
// EIP Pattern: Configuration Store + Feature Flags
// E2E: Multi-environment config routing, feature flag rollout with tenant
//      targeting, and config change notification — all via MockEndpoint.
// ============================================================================
using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_MultiEnvironmentConfigDrivenRouting()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var output = new MockEndpoint("exam-config");
        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "dev"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "prod-db.internal", "prod"));

        var environments = new[] { "dev", "staging", "prod" };
        foreach (var env in environments)
        {
            var entry = await store.GetAsync("Database:Host", env);
            Assert.That(entry, Is.Not.Null);

            var topic = $"config-{env}";
            var envelope = IntegrationEnvelope<string>.Create(
                entry!.Value, "config-store", "config.routed");
            await output.PublishAsync(envelope, topic, default);
        }

        output.AssertReceivedOnTopic("config-dev", 1);
        output.AssertReceivedOnTopic("config-staging", 1);
        output.AssertReceivedOnTopic("config-prod", 1);

        // Delete dev, others remain
        await store.DeleteAsync("Database:Host", "dev");
        Assert.That(await store.GetAsync("Database:Host", "dev"), Is.Null);
        Assert.That((await store.GetAsync("Database:Host", "prod"))!.Value,
            Is.EqualTo("prod-db.internal"));
    }

    [Test]
    public async Task Challenge2_FeatureFlagRolloutAndTenantTargeting()
    {
        await using var output = new MockEndpoint("exam-flags");
        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag(
            "BetaFeature", IsEnabled: true, RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant", "early-adopter" }));

        var tenants = new[] { "premium-tenant", "early-adopter", "regular-tenant" };
        foreach (var tenant in tenants)
        {
            var enabled = await service.IsEnabledAsync("BetaFeature", tenant);
            var topic = enabled ? "beta-access" : "standard-access";
            var envelope = IntegrationEnvelope<string>.Create(
                tenant, "feature-flags", "flag.routed");
            await output.PublishAsync(envelope, topic, default);
        }

        output.AssertReceivedOnTopic("beta-access", 2);
        output.AssertReceivedOnTopic("standard-access", 1);
    }

    [Test]
    public async Task Challenge3_ConfigChangeNotification_PublishToMockEndpoint()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var output = new MockEndpoint("exam-notify");
        var store = new InMemoryConfigurationStore(notifier);

        var changes = new List<ConfigurationChange>();
        using var subscription = notifier.Subscribe(
            new DelegateObserver<ConfigurationChange>(c => changes.Add(c)));

        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "300"));
        await store.SetAsync(new ConfigurationEntry("Cache:Ttl", "600"));
        await store.DeleteAsync("Cache:Ttl");

        // Allow async channel pump to deliver
        await Task.Delay(100);

        Assert.That(changes, Has.Count.EqualTo(3));
        Assert.That(changes[0].ChangeType, Is.EqualTo(ConfigurationChangeType.Created));
        Assert.That(changes[1].ChangeType, Is.EqualTo(ConfigurationChangeType.Updated));
        Assert.That(changes[2].ChangeType, Is.EqualTo(ConfigurationChangeType.Deleted));

        foreach (var change in changes)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{change.Key}:{change.ChangeType}", "config-store", "config.changed");
            await output.PublishAsync(envelope, "change-notifications", default);
        }

        output.AssertReceivedOnTopic("change-notifications", 3);
    }
}

file sealed class DelegateObserver<T> : IObserver<T>
{
    private readonly Action<T> _onNext;
    public DelegateObserver(Action<T> onNext) => _onNext = onNext;
    public void OnCompleted() { }
    public void OnError(Exception error) { }
    public void OnNext(T value) => _onNext(value);
}
