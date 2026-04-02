using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class ConfigurationChangeNotifierTests
{
    private ConfigurationChangeNotifier _notifier = null!;

    [SetUp]
    public void SetUp()
    {
        _notifier = new ConfigurationChangeNotifier();
    }

    [TearDown]
    public void TearDown()
    {
        _notifier.Dispose();
    }

    [Test]
    public async Task Publish_SingleSubscriber_ReceivesChange()
    {
        var received = new List<ConfigurationChange>();
        using var sub = _notifier.Subscribe(new TestObserver(received));

        _notifier.Publish(CreateChange("key1", ConfigurationChangeType.Created));

        await Task.Delay(100);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Key, Is.EqualTo("key1"));
    }

    [Test]
    public async Task Publish_MultipleSubscribers_AllReceiveChange()
    {
        var received1 = new List<ConfigurationChange>();
        var received2 = new List<ConfigurationChange>();
        var received3 = new List<ConfigurationChange>();

        using var sub1 = _notifier.Subscribe(new TestObserver(received1));
        using var sub2 = _notifier.Subscribe(new TestObserver(received2));
        using var sub3 = _notifier.Subscribe(new TestObserver(received3));

        _notifier.Publish(CreateChange("key1", ConfigurationChangeType.Updated));

        await Task.Delay(100);

        Assert.That(received1, Has.Count.EqualTo(1));
        Assert.That(received2, Has.Count.EqualTo(1));
        Assert.That(received3, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Publish_MultipleChanges_AllDeliveredInOrder()
    {
        var received = new List<ConfigurationChange>();
        using var sub = _notifier.Subscribe(new TestObserver(received));

        _notifier.Publish(CreateChange("key1", ConfigurationChangeType.Created));
        _notifier.Publish(CreateChange("key2", ConfigurationChangeType.Updated));
        _notifier.Publish(CreateChange("key3", ConfigurationChangeType.Deleted));

        await Task.Delay(100);

        Assert.That(received, Has.Count.EqualTo(3));
        Assert.That(received[0].Key, Is.EqualTo("key1"));
        Assert.That(received[1].Key, Is.EqualTo("key2"));
        Assert.That(received[2].Key, Is.EqualTo("key3"));
    }

    [Test]
    public async Task Unsubscribe_StopsReceivingChanges()
    {
        var received = new List<ConfigurationChange>();
        var sub = _notifier.Subscribe(new TestObserver(received));

        _notifier.Publish(CreateChange("key1", ConfigurationChangeType.Created));
        await Task.Delay(100);

        sub.Dispose();
        await Task.Delay(50);

        _notifier.Publish(CreateChange("key2", ConfigurationChangeType.Created));
        await Task.Delay(100);

        Assert.That(received, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Dispose_CompletesAllSubscriptions()
    {
        var observer = new TestObserver([]);
        _notifier.Subscribe(observer);

        _notifier.Dispose();

        await Task.Delay(100);

        Assert.That(observer.Completed, Is.True);
    }

    [Test]
    public async Task Publish_AfterUnsubscribe_RemainingSubscribersStillReceive()
    {
        var received1 = new List<ConfigurationChange>();
        var received2 = new List<ConfigurationChange>();

        var sub1 = _notifier.Subscribe(new TestObserver(received1));
        using var sub2 = _notifier.Subscribe(new TestObserver(received2));

        sub1.Dispose();
        await Task.Delay(50);

        _notifier.Publish(CreateChange("key1", ConfigurationChangeType.Created));
        await Task.Delay(100);

        Assert.That(received1, Has.Count.EqualTo(0));
        Assert.That(received2, Has.Count.EqualTo(1));
    }

    private static ConfigurationChange CreateChange(string key, ConfigurationChangeType type) =>
        new(key, "default", type, null, "value", DateTimeOffset.UtcNow);

    private sealed class TestObserver(List<ConfigurationChange> received) : IObserver<ConfigurationChange>
    {
        public bool Completed { get; private set; }

        public void OnCompleted() => Completed = true;
        public void OnError(Exception error) { }
        public void OnNext(ConfigurationChange value) => received.Add(value);
    }
}
