using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InMemoryMessageAggregateStoreTests
{
    private static IntegrationEnvelope<string> BuildEnvelope(
        Guid? correlationId = null,
        string payload = "item")
    {
        return IntegrationEnvelope<string>.Create(
            payload,
            source: "TestService",
            messageType: "ItemCreated",
            correlationId: correlationId);
    }

    // ------------------------------------------------------------------ //
    // AddAsync
    // ------------------------------------------------------------------ //

    [Test]
    public async Task AddAsync_SingleEnvelope_ReturnsGroupWithOneItem()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var envelope = BuildEnvelope();

        var group = await store.AddAsync(envelope);

        Assert.That(group, Has.Count.EqualTo(1));
        Assert.That(group[0].MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task AddAsync_MultipleEnvelopes_SameCorrelation_ReturnsGrownGroup()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var correlationId = Guid.NewGuid();
        var env1 = BuildEnvelope(correlationId, "a");
        var env2 = BuildEnvelope(correlationId, "b");
        var env3 = BuildEnvelope(correlationId, "c");

        await store.AddAsync(env1);
        await store.AddAsync(env2);
        var group = await store.AddAsync(env3);

        Assert.That(group, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task AddAsync_DifferentCorrelationIds_AreSeparateGroups()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();

        var group1 = await store.AddAsync(BuildEnvelope(corr1, "x"));
        var group2 = await store.AddAsync(BuildEnvelope(corr2, "y"));

        Assert.That(group1, Has.Count.EqualTo(1));
        Assert.That(group2, Has.Count.EqualTo(1));
        Assert.That(group1[0].Payload, Is.EqualTo("x"));
        Assert.That(group2[0].Payload, Is.EqualTo("y"));
    }

    [Test]
    public async Task AddAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var store = new InMemoryMessageAggregateStore<string>();

        var act = () => store.AddAsync(null!);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    // ------------------------------------------------------------------ //
    // RemoveGroupAsync
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RemoveGroupAsync_ClearsGroup_SubsequentAddStartsFresh()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var correlationId = Guid.NewGuid();

        await store.AddAsync(BuildEnvelope(correlationId, "a"));
        await store.AddAsync(BuildEnvelope(correlationId, "b"));
        await store.RemoveGroupAsync(correlationId);

        // Adding again after removal should start a new group of size 1
        var group = await store.AddAsync(BuildEnvelope(correlationId, "c"));
        Assert.That(group, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RemoveGroupAsync_NonexistentCorrelationId_DoesNotThrow()
    {
        var store = new InMemoryMessageAggregateStore<string>();

        var act = () => store.RemoveGroupAsync(Guid.NewGuid());

        Assert.DoesNotThrowAsync(async () => await act());
    }

    [Test]
    public async Task RemoveGroupAsync_OnlyRemovesTargetGroup_LeavesOthersIntact()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();

        await store.AddAsync(BuildEnvelope(corr1, "a"));
        await store.AddAsync(BuildEnvelope(corr2, "b"));
        await store.RemoveGroupAsync(corr1);

        // corr2 group should still exist and grow normally
        var group2 = await store.AddAsync(BuildEnvelope(corr2, "c"));
        Assert.That(group2, Has.Count.EqualTo(2));
    }

    // ------------------------------------------------------------------ //
    // Thread safety
    // ------------------------------------------------------------------ //

    [Test]
    public async Task AddAsync_ConcurrentAdds_SameCorrelationId_AllEnvelopesAreRecorded()
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var correlationId = Guid.NewGuid();
        const int count = 50;

        var tasks = Enumerable.Range(0, count)
            .Select(i => store.AddAsync(BuildEnvelope(correlationId, i.ToString())))
            .ToArray();

        await Task.WhenAll(tasks);

        // One final add to read the group state
        var group = await store.AddAsync(BuildEnvelope(correlationId, "last"));
        Assert.That(group, Has.Count.EqualTo(count + 1));
    }
}
