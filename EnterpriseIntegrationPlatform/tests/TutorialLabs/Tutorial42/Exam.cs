// ============================================================================
// Tutorial 42 – Configuration (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — multi environment config driven routing
//   🟡 Intermediate  — feature flag rollout and tenant targeting
//   🔴 Advanced      — config change notification_ publish to nats broker endpoint
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Configuration;
using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial42;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_MultiEnvironmentConfigDrivenRouting()
    {
        using var notifier = new ConfigurationChangeNotifier();
        await using var nats = AspireFixture.CreateNatsEndpoint("t42-exam-config");
        // TODO: Create a InMemoryConfigurationStore with appropriate configuration
        dynamic store = null!;

        await store.SetAsync(new ConfigurationEntry("Database:Host", "localhost", "dev"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "staging-db.internal", "staging"));
        await store.SetAsync(new ConfigurationEntry("Database:Host", "prod-db.internal", "prod"));

        // TODO: Create a Dictionary with appropriate configuration
        dynamic topics = null!;

        var environments = new[] { "dev", "staging", "prod" };
        foreach (var env in environments)
        {
            // TODO: var entry = await store.GetAsync(...)
            dynamic entry = null!;
            Assert.That(entry, Is.Not.Null);

            var topic = topics[env];
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
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

        // TODO: Create a InMemoryFeatureFlagService with appropriate configuration
        dynamic service = null!;

        await service.SetAsync(new FeatureFlag(
            "BetaFeature", IsEnabled: true, RolloutPercentage: 0,
            TargetTenants: new List<string> { "premium-tenant", "early-adopter" }));

        var tenants = new[] { "premium-tenant", "early-adopter", "regular-tenant" };
        foreach (var tenant in tenants)
        {
            // TODO: var enabled = await service.IsEnabledAsync(...)
            dynamic enabled = null!;
            var topic = enabled ? betaTopic : standardTopic;
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
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

        // TODO: Create a InMemoryConfigurationStore with appropriate configuration
        dynamic store = null!;

        // TODO: Create a List with appropriate configuration
        dynamic changes = null!;
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
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
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
#endif
