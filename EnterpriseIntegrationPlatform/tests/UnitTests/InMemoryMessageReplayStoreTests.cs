using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Replay;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class InMemoryMessageReplayStoreTests
{
    private static IntegrationEnvelope<string> BuildEnvelope(
        string payload = "data",
        string messageType = "TestEvent",
        Guid? correlationId = null,
        DateTimeOffset? timestamp = null)
    {
        var env = IntegrationEnvelope<string>.Create(payload, "TestService", messageType, correlationId);
        if (timestamp.HasValue)
            return env with { Timestamp = timestamp.Value };
        return env;
    }

    [Fact]
    public async Task StoreForReplayAsync_StoreAndRetrieve_ReturnsStoredEnvelope()
    {
        var store = new InMemoryMessageReplayStore();
        var envelope = BuildEnvelope("payload1");
        await store.StoreForReplayAsync(envelope, "topic1", CancellationToken.None);

        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topic1", new ReplayFilter(), 100, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(1);
        results[0].MessageId.Should().Be(envelope.MessageId);
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_FilterByCorrelationId_ReturnsMatchingOnly()
    {
        var store = new InMemoryMessageReplayStore();
        var correlationId = Guid.NewGuid();
        var env1 = BuildEnvelope("a", correlationId: correlationId);
        var env2 = BuildEnvelope("b", correlationId: Guid.NewGuid());
        await store.StoreForReplayAsync(env1, "topic", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "topic", CancellationToken.None);

        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topic", new ReplayFilter { CorrelationId = correlationId }, 100, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(1);
        results[0].CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_FilterByMessageType_ReturnsMatchingOnly()
    {
        var store = new InMemoryMessageReplayStore();
        var env1 = BuildEnvelope("a", messageType: "TypeA");
        var env2 = BuildEnvelope("b", messageType: "TypeB");
        await store.StoreForReplayAsync(env1, "topic", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "topic", CancellationToken.None);

        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topic", new ReplayFilter { MessageType = "TypeA" }, 100, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(1);
        results[0].MessageType.Should().Be("TypeA");
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_FilterByTimestampRange_ReturnsMatchingOnly()
    {
        var store = new InMemoryMessageReplayStore();
        var past = DateTimeOffset.UtcNow.AddHours(-2);
        var recent = DateTimeOffset.UtcNow;
        var env1 = BuildEnvelope("old", timestamp: past);
        var env2 = BuildEnvelope("new", timestamp: recent);
        await store.StoreForReplayAsync(env1, "topic", CancellationToken.None);
        await store.StoreForReplayAsync(env2, "topic", CancellationToken.None);

        var results = new List<IntegrationEnvelope<object>>();
        var filter = new ReplayFilter { FromTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5) };
        await foreach (var e in store.GetMessagesForReplayAsync("topic", filter, 100, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_MaxMessagesRespected_ReturnsOnlyMaxCount()
    {
        var store = new InMemoryMessageReplayStore();
        for (int i = 0; i < 10; i++)
            await store.StoreForReplayAsync(BuildEnvelope($"payload{i}"), "topic", CancellationToken.None);

        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topic", new ReplayFilter(), 3, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_EmptyStore_ReturnsNothing()
    {
        var store = new InMemoryMessageReplayStore();
        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("nonexistent", new ReplayFilter(), 100, CancellationToken.None))
            results.Add(e);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesForReplayAsync_MultipleTopics_TopicsAreIsolated()
    {
        var store = new InMemoryMessageReplayStore();
        await store.StoreForReplayAsync(BuildEnvelope("a"), "topicA", CancellationToken.None);
        await store.StoreForReplayAsync(BuildEnvelope("b"), "topicB", CancellationToken.None);

        var resultsA = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topicA", new ReplayFilter(), 100, CancellationToken.None))
            resultsA.Add(e);

        resultsA.Should().HaveCount(1);
        var resultsB = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topicB", new ReplayFilter(), 100, CancellationToken.None))
            resultsB.Add(e);

        resultsB.Should().HaveCount(1);
    }

    [Fact]
    public async Task StoreForReplayAsync_ThreadSafety_Stores100ItemsConcurrently()
    {
        var store = new InMemoryMessageReplayStore();
        var tasks = Enumerable.Range(0, 100)
            .Select(i => store.StoreForReplayAsync(BuildEnvelope($"payload{i}"), "topic", CancellationToken.None))
            .ToArray();
        await Task.WhenAll(tasks);

        var results = new List<IntegrationEnvelope<object>>();
        await foreach (var e in store.GetMessagesForReplayAsync("topic", new ReplayFilter(), 200, CancellationToken.None))
            results.Add(e);

        results.Should().HaveCount(100);
    }
}
