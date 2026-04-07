// ============================================================================
// PostgresBrokerEndpoint – Real Postgres-backed endpoint with MockEndpoint assertions
// ============================================================================
// Wraps real PostgresBrokerProducer and PostgresBrokerConsumer with the same
// assertion API as MockEndpoint/NatsBrokerEndpoint, so tests can assert
// message counts, topics, and payloads after real Postgres round-trips.
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Real PostgreSQL-backed message endpoint that provides the same
/// assertion API as <see cref="NatsBrokerEndpoint"/> and
/// <see cref="EnterpriseIntegrationPlatform.Testing.MockEndpoint"/>.
/// <para>
/// On the <b>producer</b> side it publishes to real Postgres tables.
/// On the <b>consumer</b> side it subscribes and captures received messages
/// for test assertions.
/// </para>
/// </summary>
public sealed class PostgresBrokerEndpoint : IMessageBrokerProducer, IMessageBrokerConsumer,
    IEventDrivenConsumer, IPollingConsumer, ISelectiveConsumer, IAsyncDisposable
{
    private readonly string _name;
    private readonly PostgresConnectionFactory _factory;
    private readonly PostgresBrokerProducer _producer;
    private readonly PostgresBrokerConsumer _consumer;
    private readonly ConcurrentQueue<ReceivedMessage> _published = new();
    private readonly ConcurrentQueue<ReceivedMessage> _consumed = new();
    private readonly ConcurrentQueue<object> _inbound = new();
    private bool _schemaInitialized;

    public PostgresBrokerEndpoint(string name, string connectionString)
    {
        _name = name;
        _factory = new PostgresConnectionFactory(connectionString);
        _producer = new PostgresBrokerProducer(
            _factory,
            NullLogger<PostgresBrokerProducer>.Instance);
        _consumer = new PostgresBrokerConsumer(
            _factory,
            Options.Create(new PostgresBrokerOptions
            {
                ConnectionString = connectionString,
                PollIntervalMs = 200,   // Faster polling for tests
                PollBatchSize = 100,
                LockTimeoutSeconds = 10,
            }),
            NullLogger<PostgresBrokerConsumer>.Instance);
    }

    public string Name => _name;

    /// <summary>
    /// Ensures the EIP schema tables exist. Called lazily before first publish/subscribe.
    /// </summary>
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        if (_schemaInitialized) return;
        await _factory.InitializeSchemaAsync(ct);
        _schemaInitialized = true;
    }

    // ── IMessageBrokerProducer (publishes to real Postgres) ─────────────

    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await _producer.PublishAsync(envelope, topic, cancellationToken);
        _published.Enqueue(new ReceivedMessage(envelope!, topic, DateTimeOffset.UtcNow));
    }

    // ── IMessageBrokerConsumer (subscribes on real Postgres) ────────────

    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await _consumer.SubscribeAsync<T>(
            topic, consumerGroup,
            async env =>
            {
                _consumed.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
                _inbound.Enqueue(env!);
                await handler(env);
            },
            cancellationToken);
    }

    // ── IEventDrivenConsumer ────────────────────────────────────────────

    public Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default) =>
        SubscribeAsync(topic, consumerGroup, handler, cancellationToken);

    // ── IPollingConsumer ────────────────────────────────────────────────

    public async Task<IReadOnlyList<IntegrationEnvelope<T>>> PollAsync<T>(
        string topic,
        string consumerGroup,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var results = await _consumer.PollAsync<T>(topic, consumerGroup, maxMessages, cancellationToken);
        foreach (var env in results)
        {
            _consumed.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
            _inbound.Enqueue(env!);
        }
        return results;
    }

    // ── ISelectiveConsumer ──────────────────────────────────────────────

    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await _consumer.SubscribeAsync<T>(
            topic, consumerGroup, predicate,
            async env =>
            {
                _consumed.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
                _inbound.Enqueue(env!);
                await handler(env);
            },
            cancellationToken);
    }

    // ── Test helpers: send messages (publishes to real Postgres) ────────

    /// <summary>
    /// Sends a test message through real Postgres, triggering any registered subscribers.
    /// </summary>
    public async Task SendAsync<T>(IntegrationEnvelope<T> envelope, string topic = "test-input")
    {
        await EnsureSchemaAsync();
        await _producer.PublishAsync(envelope, topic, CancellationToken.None);
    }

    // ── Assertions (same API as MockEndpoint/NatsBrokerEndpoint) ────────

    /// <summary>All messages published through this endpoint.</summary>
    public IReadOnlyList<ReceivedMessage> Received => _published.ToArray();

    /// <summary>Number of messages published.</summary>
    public int ReceivedCount => _published.Count;

    public IntegrationEnvelope<T> GetReceived<T>(int index = 0) =>
        (IntegrationEnvelope<T>)_published.ElementAt(index).Envelope;

    public IReadOnlyList<IntegrationEnvelope<T>> GetAllReceived<T>(string? topic = null) =>
        _published
            .Where(r => topic is null || r.Topic == topic)
            .Select(r => (IntegrationEnvelope<T>)r.Envelope)
            .ToList();

    public IReadOnlyList<string> GetReceivedTopics() =>
        _published.Select(r => r.Topic).Distinct().ToList();

    public void AssertReceivedCount(int expected) =>
        Assert.That(_published.Count, Is.EqualTo(expected),
            $"PostgresBrokerEndpoint '{_name}': expected {expected} message(s), published {_published.Count}");

    public void AssertNoneReceived() =>
        Assert.That(_published.Count, Is.EqualTo(0),
            $"PostgresBrokerEndpoint '{_name}': expected no messages, published {_published.Count}");

    public void AssertReceivedOnTopic(string topic, int expected) =>
        Assert.That(
            _published.Count(r => r.Topic == topic),
            Is.EqualTo(expected),
            $"PostgresBrokerEndpoint '{_name}': expected {expected} on '{topic}'");

    // ── Consumer-side assertions (messages received from real Postgres) ──

    /// <summary>All messages consumed from real Postgres subscriptions.</summary>
    public IReadOnlyList<ReceivedMessage> Consumed => _consumed.ToArray();

    /// <summary>Number of messages consumed from real Postgres.</summary>
    public int ConsumedCount => _consumed.Count;

    public IntegrationEnvelope<T> GetConsumed<T>(int index = 0) =>
        (IntegrationEnvelope<T>)_consumed.ElementAt(index).Envelope;

    /// <summary>
    /// Polls until the expected published count is reached or timeout expires.
    /// </summary>
    public async Task WaitForMessagesAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (_published.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Polls until the expected published count on a specific topic is reached.
    /// </summary>
    public async Task WaitForMessagesOnTopicAsync(string topic, int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (_published.Count(r => r.Topic == topic) < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Polls until the expected consumed count is reached or timeout expires.
    /// Used for tests that verify real Postgres delivery to subscribers.
    /// </summary>
    public async Task WaitForConsumedAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (_consumed.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    public void Reset()
    {
        while (_published.TryDequeue(out _)) { }
        while (_consumed.TryDequeue(out _)) { }
        while (_inbound.TryDequeue(out _)) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _consumer.DisposeAsync();
        await _factory.DisposeAsync();
        Reset();
    }

    public sealed record ReceivedMessage(object Envelope, string Topic, DateTimeOffset ReceivedAt);
}
