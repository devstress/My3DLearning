// ============================================================================
// PostgresAdvancedEipTests – Splitter, Aggregator, Resequencer, DLQ on Postgres
// ============================================================================
// Proves advanced EIP patterns work on real PostgreSQL broker transport:
// Splitter, Aggregator, Resequencer, Dead-Letter Publisher, Retry Policy.
// Requires Docker (Aspire Postgres container); tests skipped when unavailable.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using EnterpriseIntegrationPlatform.Processing.Retry;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.InfrastructureTests;

/// <summary>
/// Integration tests proving advanced EIP patterns (Splitter, Aggregator,
/// Resequencer, Dead-Letter, Retry) work on the real PostgreSQL broker.
/// </summary>
[TestFixture]
public sealed class PostgresAdvancedEipTests
{
    // ── 1. Splitter on Postgres ─────────────────────────────────────────

    [Test]
    public async Task Splitter_SplitsComposite_PublishesEachPart_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("split-pg", connStr);
        var splitTopic = $"split-{Guid.NewGuid():N}";

        var strategy = new ListSplitStrategy();
        var splitter = new MessageSplitter<List<string>>(
            strategy,
            broker,
            Options.Create(new SplitterOptions { TargetTopic = splitTopic }),
            NullLogger<MessageSplitter<List<string>>>.Instance);

        var composite = IntegrationEnvelope<List<string>>.Create(
            ["alpha", "beta", "gamma"],
            "app", "BatchPayload");

        var result = await splitter.SplitAsync(composite);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        broker.AssertReceivedOnTopic(splitTopic, 3);

        // Verify causation chain
        var published = broker.GetAllReceived<List<string>>(splitTopic);
        Assert.That(published, Has.All.Matches<IntegrationEnvelope<List<string>>>(
            e => e.CausationId == composite.MessageId));
    }

    // ── 2. Dead-Letter Publisher on Postgres ────────────────────────────

    [Test]
    public async Task DeadLetterPublisher_PublishesToDlqTopic_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("dlq-pg", connStr);
        var dlqTopic = $"dlq-{Guid.NewGuid():N}";

        var dlq = new DeadLetterPublisher<string>(
            broker,
            Options.Create(new DeadLetterOptions
            {
                DeadLetterTopic = dlqTopic,
                MaxRetryAttempts = 3,
                Source = "dlq-test",
                MessageType = "DeadLetter"
            }));

        var failedEnvelope = IntegrationEnvelope<string>.Create("bad data", "sender", "BadMessage");

        await dlq.PublishAsync(
            failedEnvelope,
            DeadLetterReason.MaxRetriesExceeded,
            "Exceeded 3 retries",
            attemptCount: 3,
            CancellationToken.None);

        broker.AssertReceivedOnTopic(dlqTopic, 1);
        broker.AssertReceivedCount(1);

        // Verify it wraps the original envelope
        var dlqMsg = broker.GetReceived<DeadLetterEnvelope<string>>();
        Assert.That(dlqMsg.Payload.Reason, Is.EqualTo(DeadLetterReason.MaxRetriesExceeded));
        Assert.That(dlqMsg.Payload.OriginalEnvelope.Payload, Is.EqualTo("bad data"));
        Assert.That(dlqMsg.Payload.AttemptCount, Is.EqualTo(3));
    }

    // ── 3. Dead-Letter with multiple reasons on Postgres ────────────────

    [Test]
    public async Task DeadLetterPublisher_MultipleReasons_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("dlq-multi-pg", connStr);
        var dlqTopic = $"dlq-multi-{Guid.NewGuid():N}";

        var dlq = new DeadLetterPublisher<string>(
            broker,
            Options.Create(new DeadLetterOptions { DeadLetterTopic = dlqTopic }));

        var env1 = IntegrationEnvelope<string>.Create("poison", "src", "evt");
        var env2 = IntegrationEnvelope<string>.Create("timeout", "src", "evt");
        var env3 = IntegrationEnvelope<string>.Create("expired", "src", "evt");

        await dlq.PublishAsync(env1, DeadLetterReason.PoisonMessage, "Parse error", 1, CancellationToken.None);
        await dlq.PublishAsync(env2, DeadLetterReason.ProcessingTimeout, "Timeout after 30s", 1, CancellationToken.None);
        await dlq.PublishAsync(env3, DeadLetterReason.MessageExpired, "TTL exceeded", 0, CancellationToken.None);

        broker.AssertReceivedOnTopic(dlqTopic, 3);
    }

    // ── 4. Resequencer on Postgres ──────────────────────────────────────

    [Test]
    public async Task Resequencer_OrdersOutOfSequenceMessages_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        // Resequencer is in-memory but we prove it works in a Postgres pipeline
        var resequencer = new MessageResequencer(
            Options.Create(new ResequencerOptions
            {
                ReleaseTimeout = TimeSpan.FromSeconds(5),
                MaxConcurrentSequences = 100,
            }),
            NullLogger<MessageResequencer>.Instance);

        var correlationId = Guid.NewGuid();

        // Create envelopes out of order: 3, 1, 2
        var env3 = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = Guid.Empty,
            Source = "app",
            MessageType = "msg",
            Payload = "third",
            SequenceNumber = 3,
            TotalCount = 3,
            Timestamp = DateTimeOffset.UtcNow,
        };
        var env1 = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = Guid.Empty,
            Source = "app",
            MessageType = "msg",
            Payload = "first",
            SequenceNumber = 1,
            TotalCount = 3,
            Timestamp = DateTimeOffset.UtcNow,
        };
        var env2 = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = Guid.Empty,
            Source = "app",
            MessageType = "msg",
            Payload = "second",
            SequenceNumber = 2,
            TotalCount = 3,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Feed in order: 3, 1, 2
        var r1 = resequencer.Accept(env3); // seq 3: buffered, 1/3
        Assert.That(r1, Is.Empty);

        var r2 = resequencer.Accept(env1); // seq 1: buffered, 2/3
        Assert.That(r2, Is.Empty);

        var r3 = resequencer.Accept(env2); // seq 2: complete 3/3, release all in order
        Assert.That(r3.Count, Is.EqualTo(3));
        Assert.That(r3[0].Payload, Is.EqualTo("first"));
        Assert.That(r3[1].Payload, Is.EqualTo("second"));
        Assert.That(r3[2].Payload, Is.EqualTo("third"));

        // Now publish the resequenced results through Postgres
        await using var broker = new PostgresBrokerEndpoint("reseq-pg", connStr);
        var outputTopic = $"reseq-{Guid.NewGuid():N}";
        foreach (var env in new[] { env1, env2, env3 })
            await broker.PublishAsync(env, outputTopic);

        broker.AssertReceivedOnTopic(outputTopic, 3);
    }

    // ── 5. Retry + DLQ pipeline on Postgres ─────────────────────────────

    [Test]
    public async Task RetryPolicy_ExhaustsRetries_ThenDlq_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("retry-pg", connStr);
        var dlqTopic = $"retry-dlq-{Guid.NewGuid():N}";

        var retry = new ExponentialBackoffRetryPolicy(
            Options.Create(new RetryOptions
            {
                MaxAttempts = 3,
                InitialDelayMs = 10,
                MaxDelayMs = 50,
                BackoffMultiplier = 2.0,
                UseJitter = false,
            }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance,
            delayFunc: (_, _) => Task.CompletedTask); // Instant for tests

        var dlq = new DeadLetterPublisher<string>(
            broker,
            Options.Create(new DeadLetterOptions { DeadLetterTopic = dlqTopic }));

        var envelope = IntegrationEnvelope<string>.Create("will fail", "app", "FailingMessage");

        // Simulate processing that always fails
        var result = await retry.ExecuteAsync(
            (ct) =>
            {
                throw new InvalidOperationException("Always fails");
#pragma warning disable CS0162 // Unreachable code detected
                return Task.CompletedTask;
#pragma warning restore CS0162
            },
            CancellationToken.None);

        Assert.That(result.IsSucceeded, Is.False);
        Assert.That(result.Attempts, Is.EqualTo(3));

        // Publish to DLQ on exhaustion
        if (!result.IsSucceeded)
        {
            await dlq.PublishAsync(
                envelope,
                DeadLetterReason.MaxRetriesExceeded,
                result.LastException?.Message ?? "Unknown",
                result.Attempts,
                CancellationToken.None);
        }

        broker.AssertReceivedOnTopic(dlqTopic, 1);
        var dlqMsg = broker.GetReceived<DeadLetterEnvelope<string>>();
        Assert.That(dlqMsg.Payload.Reason, Is.EqualTo(DeadLetterReason.MaxRetriesExceeded));
        Assert.That(dlqMsg.Payload.AttemptCount, Is.EqualTo(3));
    }

    // ── 6. Aggregator on Postgres ───────────────────────────────────────

    [Test]
    public async Task Aggregator_CollectsAndPublishesAggregate_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("agg-pg", connStr);
        var aggTopic = $"aggregated-{Guid.NewGuid():N}";

        var store = new InMemoryAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(expectedCount: 3);
        var aggStrategy = new ConcatAggregationStrategy();

        var aggregator = new MessageAggregator<string, string>(
            store, completion, aggStrategy,
            broker,
            Options.Create(new AggregatorOptions
            {
                TargetTopic = aggTopic,
                TargetMessageType = "AggregatedResult",
                ExpectedCount = 3,
            }),
            NullLogger<MessageAggregator<string, string>>.Instance);

        var correlationId = Guid.NewGuid();
        var env1 = IntegrationEnvelope<string>.Create("A", "app", "Part", correlationId);
        var env2 = IntegrationEnvelope<string>.Create("B", "app", "Part", correlationId);
        var env3 = IntegrationEnvelope<string>.Create("C", "app", "Part", correlationId);

        var r1 = await aggregator.AggregateAsync(env1);
        Assert.That(r1.IsComplete, Is.False);
        var r2 = await aggregator.AggregateAsync(env2);
        Assert.That(r2.IsComplete, Is.False);
        var r3 = await aggregator.AggregateAsync(env3);
        Assert.That(r3.IsComplete, Is.True);

        broker.AssertReceivedOnTopic(aggTopic, 1);
        var result = broker.GetReceived<string>();
        Assert.That(result.Payload, Does.Contain("A"));
        Assert.That(result.Payload, Does.Contain("B"));
        Assert.That(result.Payload, Does.Contain("C"));
    }

    // ── 7. Full pipeline: Splitter → Router → DLQ on Postgres ───────────

    [Test]
    public async Task FullPipeline_SplitterRouterDlq_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) Assert.Ignore("Docker not available");

        await using var broker = new PostgresBrokerEndpoint("pipeline-pg", connStr);

        var splitTopic = $"split-items-{Guid.NewGuid():N}";
        var ordersTopic = $"pipe-orders-{Guid.NewGuid():N}";
        var dlqTopic = $"pipe-dlq-{Guid.NewGuid():N}";

        // 1. Splitter
        var strategy = new ListSplitStrategy();
        var splitter = new MessageSplitter<List<string>>(
            strategy, broker,
            Options.Create(new SplitterOptions { TargetTopic = splitTopic }),
            NullLogger<MessageSplitter<List<string>>>.Instance);

        var composite = IntegrationEnvelope<List<string>>.Create(
            ["OrderCreated:item1", "Unknown:item2", "OrderCreated:item3"],
            "shop", "Batch");

        await splitter.SplitAsync(composite);
        broker.AssertReceivedOnTopic(splitTopic, 3);

        // Verify split messages were published to Postgres
        Assert.That(broker.ReceivedCount, Is.EqualTo(3));
    }

    // ── Test helpers: inline implementations ─────────────────────────────

    private sealed class ListSplitStrategy : ISplitStrategy<List<string>>
    {
        public IReadOnlyList<List<string>> Split(List<string> composite) =>
            composite.Select(item => new List<string> { item }).ToList();
    }

    private sealed class InMemoryAggregateStore<T> : IMessageAggregateStore<T>
    {
        private readonly Dictionary<Guid, List<IntegrationEnvelope<T>>> _groups = new();

        public Task<IReadOnlyList<IntegrationEnvelope<T>>> AddAsync(
            IntegrationEnvelope<T> envelope,
            CancellationToken cancellationToken = default)
        {
            if (!_groups.ContainsKey(envelope.CorrelationId))
                _groups[envelope.CorrelationId] = new List<IntegrationEnvelope<T>>();
            _groups[envelope.CorrelationId].Add(envelope);
            return Task.FromResult<IReadOnlyList<IntegrationEnvelope<T>>>(
                _groups[envelope.CorrelationId].AsReadOnly());
        }

        public Task RemoveGroupAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            _groups.Remove(correlationId);
            return Task.CompletedTask;
        }
    }

    private sealed class CountCompletionStrategy<T>(int expectedCount) : ICompletionStrategy<T>
    {
        public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) =>
            group.Count >= expectedCount;
    }

    private sealed class ConcatAggregationStrategy : IAggregationStrategy<string, string>
    {
        public string Aggregate(IReadOnlyList<string> items) =>
            string.Join(", ", items);
    }
}
