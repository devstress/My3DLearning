// ============================================================================
// KafkaBrokerEndpoint – Real Kafka-backed endpoint with MockEndpoint assertions
// ============================================================================
// Wraps real KafkaProducer and KafkaConsumer with the same assertion API as
// MockEndpoint/NatsBrokerEndpoint, so tests can assert message counts, topics,
// and payloads after real Kafka round-trips.
// ============================================================================

using System.Collections.Concurrent;
using Confluent.Kafka;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Real Apache Kafka-backed message endpoint that provides the same
/// assertion API as <see cref="NatsBrokerEndpoint"/> and
/// <see cref="EnterpriseIntegrationPlatform.Testing.MockEndpoint"/>.
/// <para>
/// On the <b>producer</b> side it publishes to real Kafka topics.
/// On the <b>consumer</b> side it subscribes and captures received messages
/// for test assertions.
/// </para>
/// </summary>
public sealed class KafkaBrokerEndpoint : IMessageBrokerProducer, IMessageBrokerConsumer, IAsyncDisposable
{
    private readonly string _name;
    private readonly string _bootstrapServers;
    private readonly IProducer<string, byte[]> _rawProducer;
    private readonly KafkaProducer _producer;
    private readonly ConcurrentQueue<ReceivedMessage> _published = new();
    private readonly ConcurrentQueue<ReceivedMessage> _consumed = new();
    private readonly List<CancellationTokenSource> _subscriptionTokens = new();

    public KafkaBrokerEndpoint(string name, string bootstrapServers)
    {
        _name = name;
        _bootstrapServers = bootstrapServers;
        _rawProducer = new ProducerBuilder<string, byte[]>(
            new ProducerConfig { BootstrapServers = bootstrapServers }).Build();
        _producer = new KafkaProducer(_rawProducer, NullLogger<KafkaProducer>.Instance);
    }

    public string Name => _name;

    // ── IMessageBrokerProducer (publishes to real Kafka) ────────────────

    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        await _producer.PublishAsync(envelope, topic, cancellationToken);
        _published.Enqueue(new ReceivedMessage(envelope!, topic, DateTimeOffset.UtcNow));
    }

    // ── IMessageBrokerConsumer (subscribes on real Kafka) ───────────────

    public Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _subscriptionTokens.Add(cts);

        // Run consumption in background
        _ = Task.Run(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = consumerGroup + "-" + Guid.NewGuid().ToString("N")[..8],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
            };

            using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
            consumer.Subscribe(topic);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(cts.Token);
                        if (result?.Message?.Value is null) continue;

                        var env = EnvelopeSerializer.Deserialize<T>(result.Message.Value);
                        if (env is not null)
                        {
                            await handler(env);
                            _consumed.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
                        }
                        consumer.Commit(result);
                    }
                    catch (ConsumeException) { }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                consumer.Close();
            }
        }, cts.Token);

        return Task.CompletedTask;
    }

    // ── Test helpers: send messages (publishes to real Kafka) ───────────

    /// <summary>
    /// Sends a test message through real Kafka, triggering any registered subscribers.
    /// </summary>
    public async Task SendAsync<T>(IntegrationEnvelope<T> envelope, string topic = "test-input")
    {
        await _producer.PublishAsync(envelope, topic, CancellationToken.None);
    }

    // ── Assertions (same API as NatsBrokerEndpoint) ─────────────────────

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
            $"KafkaBrokerEndpoint '{_name}': expected {expected} message(s), published {_published.Count}");

    public void AssertNoneReceived() =>
        Assert.That(_published.Count, Is.EqualTo(0),
            $"KafkaBrokerEndpoint '{_name}': expected no messages, published {_published.Count}");

    public void AssertReceivedOnTopic(string topic, int expected) =>
        Assert.That(
            _published.Count(r => r.Topic == topic),
            Is.EqualTo(expected),
            $"KafkaBrokerEndpoint '{_name}': expected {expected} on '{topic}'");

    // ── Consumer-side assertions (messages received from real Kafka) ─────

    /// <summary>All messages consumed from real Kafka subscriptions.</summary>
    public IReadOnlyList<ReceivedMessage> Consumed => _consumed.ToArray();

    /// <summary>Number of messages consumed from real Kafka.</summary>
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
    /// Polls until the expected consumed count is reached or timeout expires.
    /// </summary>
    public async Task WaitForConsumedAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (_consumed.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }
    }

    public void Reset()
    {
        while (_published.TryDequeue(out _)) { }
        while (_consumed.TryDequeue(out _)) { }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _subscriptionTokens)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        _subscriptionTokens.Clear();
        _rawProducer.Dispose();
        Reset();
    }

    public sealed record ReceivedMessage(object Envelope, string Topic, DateTimeOffset ReceivedAt);
}
