using EnterpriseIntegrationPlatform.Configuration;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class InMemoryConfigurationStoreTests
{
    private ConfigurationChangeNotifier _notifier = null!;
    private InMemoryConfigurationStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _notifier = new ConfigurationChangeNotifier();
        _store = new InMemoryConfigurationStore(_notifier);
    }

    [TearDown]
    public void TearDown()
    {
        _notifier.Dispose();
    }

    [Test]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        var result = await _store.GetAsync("missing-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_NewEntry_CreatesWithVersion1()
    {
        var entry = new ConfigurationEntry("db:host", "localhost");

        var stored = await _store.SetAsync(entry);

        Assert.That(stored.Key, Is.EqualTo("db:host"));
        Assert.That(stored.Value, Is.EqualTo("localhost"));
        Assert.That(stored.Version, Is.EqualTo(1));
    }

    [Test]
    public async Task SetAsync_ExistingEntry_IncrementsVersion()
    {
        await _store.SetAsync(new ConfigurationEntry("db:host", "localhost"));

        var updated = await _store.SetAsync(new ConfigurationEntry("db:host", "remotehost"));

        Assert.That(updated.Version, Is.EqualTo(2));
        Assert.That(updated.Value, Is.EqualTo("remotehost"));
    }

    [Test]
    public async Task GetAsync_AfterSet_ReturnsStoredEntry()
    {
        await _store.SetAsync(new ConfigurationEntry("db:port", "5432"));

        var result = await _store.GetAsync("db:port");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("5432"));
    }

    [Test]
    public async Task DeleteAsync_ExistingEntry_ReturnsTrue()
    {
        await _store.SetAsync(new ConfigurationEntry("temp:key", "value"));

        var deleted = await _store.DeleteAsync("temp:key");

        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task DeleteAsync_NonExistentEntry_ReturnsFalse()
    {
        var deleted = await _store.DeleteAsync("nonexistent");

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task DeleteAsync_AfterDelete_GetReturnsNull()
    {
        await _store.SetAsync(new ConfigurationEntry("temp:key", "value"));
        await _store.DeleteAsync("temp:key");

        var result = await _store.GetAsync("temp:key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ListAsync_NoFilter_ReturnsAllEntries()
    {
        await _store.SetAsync(new ConfigurationEntry("key1", "v1", "dev"));
        await _store.SetAsync(new ConfigurationEntry("key2", "v2", "prod"));
        await _store.SetAsync(new ConfigurationEntry("key3", "v3", "default"));

        var all = await _store.ListAsync();

        Assert.That(all, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ListAsync_WithEnvironmentFilter_ReturnsMatchingOnly()
    {
        await _store.SetAsync(new ConfigurationEntry("key1", "v1", "dev"));
        await _store.SetAsync(new ConfigurationEntry("key2", "v2", "prod"));
        await _store.SetAsync(new ConfigurationEntry("key3", "v3", "dev"));

        var devEntries = await _store.ListAsync("dev");

        Assert.That(devEntries, Has.Count.EqualTo(2));
        Assert.That(devEntries.All(e => e.Environment == "dev"), Is.True);
    }

    [Test]
    public async Task SetAsync_DifferentEnvironments_SameKey_StoresSeparately()
    {
        await _store.SetAsync(new ConfigurationEntry("db:host", "localhost", "dev"));
        await _store.SetAsync(new ConfigurationEntry("db:host", "prod-server", "prod"));

        var devEntry = await _store.GetAsync("db:host", "dev");
        var prodEntry = await _store.GetAsync("db:host", "prod");

        Assert.That(devEntry!.Value, Is.EqualTo("localhost"));
        Assert.That(prodEntry!.Value, Is.EqualTo("prod-server"));
    }

    [Test]
    public async Task WatchAsync_OnSet_NotifiesSubscribers()
    {
        var observer = new ChangeCollector();
        using var subscription = _store.WatchAsync().Subscribe(observer);

        await _store.SetAsync(new ConfigurationEntry("key", "value"));

        // Allow the channel pump to deliver
        await Task.Delay(100);

        Assert.That(observer.Changes, Has.Count.EqualTo(1));
        Assert.That(observer.Changes[0].ChangeType, Is.EqualTo(ConfigurationChangeType.Created));
        Assert.That(observer.Changes[0].Key, Is.EqualTo("key"));
    }

    [Test]
    public async Task WatchAsync_OnUpdate_NotifiesWithOldAndNewValue()
    {
        var observer = new ChangeCollector();
        using var subscription = _store.WatchAsync().Subscribe(observer);

        await _store.SetAsync(new ConfigurationEntry("key", "v1"));
        await _store.SetAsync(new ConfigurationEntry("key", "v2"));

        await Task.Delay(100);

        Assert.That(observer.Changes, Has.Count.EqualTo(2));
        Assert.That(observer.Changes[1].ChangeType, Is.EqualTo(ConfigurationChangeType.Updated));
        Assert.That(observer.Changes[1].OldValue, Is.EqualTo("v1"));
        Assert.That(observer.Changes[1].NewValue, Is.EqualTo("v2"));
    }

    [Test]
    public async Task WatchAsync_OnDelete_NotifiesWithDeletedType()
    {
        await _store.SetAsync(new ConfigurationEntry("key", "value"));

        var observer = new ChangeCollector();
        using var subscription = _store.WatchAsync().Subscribe(observer);

        await _store.DeleteAsync("key");

        await Task.Delay(100);

        Assert.That(observer.Changes, Has.Count.EqualTo(1));
        Assert.That(observer.Changes[0].ChangeType, Is.EqualTo(ConfigurationChangeType.Deleted));
    }

    [Test]
    public async Task SetAsync_ConcurrentAccess_MaintainsConsistency()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _store.SetAsync(new ConfigurationEntry($"concurrent:key{i}", $"value{i}")));

        await Task.WhenAll(tasks);

        var all = await _store.ListAsync();
        Assert.That(all, Has.Count.EqualTo(100));
    }

    [Test]
    public async Task SetAsync_ConcurrentUpdatesToSameKey_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            _store.SetAsync(new ConfigurationEntry("shared:key", $"value{i}")));

        await Task.WhenAll(tasks);

        var result = await _store.GetAsync("shared:key");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Version, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task SetAsync_SetsModifiedBy()
    {
        var entry = new ConfigurationEntry("key", "value", ModifiedBy: "admin-user");

        var stored = await _store.SetAsync(entry);

        Assert.That(stored.ModifiedBy, Is.EqualTo("admin-user"));
    }

    /// <summary>Helper observer for collecting changes.</summary>
    private sealed class ChangeCollector : IObserver<ConfigurationChange>
    {
        public List<ConfigurationChange> Changes { get; } = [];
        public bool Completed { get; private set; }
        public Exception? Error { get; private set; }

        public void OnCompleted() => Completed = true;
        public void OnError(Exception error) => Error = error;
        public void OnNext(ConfigurationChange value) => Changes.Add(value);
    }
}
