// ============================================================================
// Tutorial 42 – Configuration (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — multi environment config driven routing
//   🟡 Intermediate — feature flag rollout and tenant targeting
//   🔴 Advanced     — config change notification_ publish to nats broker endpoint
// ============================================================================

using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_MultiEnvironmentConfigDrivenRouting()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-exam-config");
        var store = new InMemoryConfigurationStore(notifier);

        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "dev"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "prod-db.internal", "prod"));

        var topics = new Dictionary<string, string>
        {
            ["dev"] = AspireFixture.UniqueTopic("t42-exam-config-dev"),
            ["staging"] = AspireFixture.UniqueTopic("t42-exam-config-staging"),
            ["prod"] = AspireFixture.UniqueTopic("t42-exam-config-prod"),
        };

        var environments = new[] { "dev", "staging", "prod" };
        foreach (var env in environments)
        {
            var entry = await store.GetAsync("Database:Host", env);
            Assert.That(entry, Is.Not.Null);

            var topic = topics[env];
            var envelope = IntegrationEnvelope<string>.Create(
                entry!.Value, "config-store", "config.routed");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topics["dev"], 1);
        nats.AssertReceivedOnTopic(topics["staging"], 1);
        nats.AssertReceivedOnTopic(topics["prod"], 1);

        // Delete dev, others remain
        await store.DeleteAsync("Database:Host", "dev");
        Assert.That(await store.GetAsync("Database:Host", "dev"), Is.Null);
        Assert.That((await store.GetAsync("Database:Host", "prod"))!.Value,
            Is.EqualTo("prod-db.internal"));
    }

    [Test]
    public async Task Intermediate_FeatureFlagRolloutAndTenantTargeting()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-exam-flags");
        var betaTopic = AspireFixture.UniqueTopic("t42-exam-beta-access");
        var standardTopic = AspireFixture.UniqueTopic("t42-exam-standard-access");

        var service = new InMemoryFeatureFlagService();

        await service.SetAsync(new FeatureFlag(
            "BetaFeature", IsEnabled: true, RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant", "early-adopter" }));

        var tenants = new[] { "premium-tenant", "early-adopter", "regular-tenant" };
        foreach (var tenant in tenants)
        {
            var enabled = await service.IsEnabledAsync("BetaFeature", tenant);
            var topic = enabled ? betaTopic : standardTopic;
            var envelope = IntegrationEnvelope<string>.Create(
                tenant, "feature-flags", "flag.routed");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(betaTopic, 2);
        nats.AssertReceivedOnTopic(standardTopic, 1);
    }

    [Test]
    public async Task Advanced_ConfigChangeNotification_PublishToNatsBrokerEndpoint()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-exam-notify");
        var topic = AspireFixture.UniqueTopic("t42-exam-change-notifications");

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
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
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
