using EnterpriseIntegrationPlatform.EventSourcing;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.EventSourcingTests;

[TestFixture]
public class InMemorySnapshotStoreTests
{
    private InMemorySnapshotStore<int> _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemorySnapshotStore<int>();
    }

    [Test]
    public async Task LoadAsync_NoSnapshot_ReturnsDefaultAndVersionZero()
    {
        var (state, version) = await _store.LoadAsync("nonexistent");

        Assert.That(state, Is.EqualTo(default(int)));
        Assert.That(version, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveAsync_ThenLoad_ReturnsSavedStateAndVersion()
    {
        await _store.SaveAsync("s1", 42, 5);

        var (state, version) = await _store.LoadAsync("s1");

        Assert.That(state, Is.EqualTo(42));
        Assert.That(version, Is.EqualTo(5));
    }

    [Test]
    public async Task SaveAsync_Overwrite_ReturnsLatestSnapshot()
    {
        await _store.SaveAsync("s1", 10, 3);
        await _store.SaveAsync("s1", 20, 7);

        var (state, version) = await _store.LoadAsync("s1");

        Assert.That(state, Is.EqualTo(20));
        Assert.That(version, Is.EqualTo(7));
    }

    [Test]
    public async Task SaveAsync_MultipleStreams_IndependentSnapshots()
    {
        await _store.SaveAsync("s1", 100, 10);
        await _store.SaveAsync("s2", 200, 20);

        var (s1State, s1Version) = await _store.LoadAsync("s1");
        var (s2State, s2Version) = await _store.LoadAsync("s2");

        Assert.That(s1State, Is.EqualTo(100));
        Assert.That(s1Version, Is.EqualTo(10));
        Assert.That(s2State, Is.EqualTo(200));
        Assert.That(s2Version, Is.EqualTo(20));
    }

    [Test]
    public void SaveAsync_NullStreamId_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.SaveAsync(null!, 1, 1));
    }

    [Test]
    public void LoadAsync_NullStreamId_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.LoadAsync(null!));
    }

    [Test]
    public async Task SaveAsync_ReferenceTypeState_StoresCorrectly()
    {
        var refStore = new InMemorySnapshotStore<string>();
        await refStore.SaveAsync("s1", "hello", 3);

        var (state, version) = await refStore.LoadAsync("s1");

        Assert.That(state, Is.EqualTo("hello"));
        Assert.That(version, Is.EqualTo(3));
    }
}
