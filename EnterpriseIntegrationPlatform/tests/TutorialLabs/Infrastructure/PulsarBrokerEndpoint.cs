// ============================================================================
// PulsarBrokerEndpoint – Real Pulsar-backed endpoint with MockEndpoint assertions
// ============================================================================
// Wraps real PulsarProducer and PulsarConsumer with the same assertion API as
// MockEndpoint/NatsBrokerEndpoint, so tests can assert message counts, topics,
// and payloads after real Pulsar round-trips.
// ============================================================================

using System.Buffers;
using System.Collections.Concurrent;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Real Apache Pulsar-backed message endpoint that provides the same
/// assertion API as <see cref="NatsBrokerEndpoint"/> and
/// <see cref="EnterpriseIntegrationPlatform.Testing.MockEndpoint"/>.
/// <para>
/// On the <b>producer</b> side it publishes to real Pulsar topics.
/// On the <b>consumer</b> side it subscribes and captures received messages
/// for test assertions.
/// </para>
/// </summary>
public sealed class PulsarBrokerEndpoint : IMessageBrokerProducer, IMessageBrokerConsumer, IAsyncDisposable
{
    private readonly string _name;
    private readonly IPulsarClient _client;
    private readonly PulsarProducer _producer;
    private readonly ConcurrentQueue<ReceivedMessage> _published = new();
    private readonly ConcurrentQueue<ReceivedMessage> _consumed = new();
    private readonly List<CancellationTokenSource> _subscriptionTokens = new();

    public PulsarBrokerEndpoint(string name, string serviceUrl)
    {
        _name = name;
        _client = PulsarClient.Builder()
            .ServiceUrl(new Uri(serviceUrl))
            .Build();
        _producer = new PulsarProducer(_client, NullLogger<PulsarProducer>.Instance);
    }

    public string Name => _name;

    // ── IMessageBrokerProducer (publishes to real Pulsar) ───────────────

    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        await _producer.PublishAsync(envelope, topic, cancellationToken);
        _published.Enqueue(new ReceivedMessage(envelope!, topic, DateTimeOffset.UtcNow));
    }

    // ── IMessageBrokerConsumer (subscribes on real Pulsar) ──────────────

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
            var consumer = _client.NewConsumer()
                .SubscriptionName(consumerGroup + "-" + Guid.NewGuid().ToString("N")[..8])
                .Topic(topic)
                .SubscriptionType(SubscriptionType.KeyShared)
                .Create();

            try
            {
                await using (consumer.ConfigureAwait(false))
                {
                    await foreach (var msg in consumer.Messages(cts.Token))
                    {
                        try
                        {
                            var bytes = msg.Data.IsSingleSegment
                                ? msg.Data.FirstSpan
                                : msg.Data.ToArray();
                            var env = EnvelopeSerializer.Deserialize<T>(bytes);
                            if (env is not null)
                            {
                                await handler(env);
                                _consumed.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
                            }
                            await consumer.Acknowledge(msg, cts.Token);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            await consumer.RedeliverUnacknowledgedMessages(
                                [msg.MessageId], cts.Token);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, cts.Token);

        return Task.CompletedTask;
    }

    // ── Test helpers: send messages (publishes to real Pulsar) ──────────

    /// <summary>
    /// Sends a test message through real Pulsar, triggering any registered subscribers.
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
            $"PulsarBrokerEndpoint '{_name}': expected {expected} message(s), published {_published.Count}");

    public void AssertNoneReceived() =>
        Assert.That(_published.Count, Is.EqualTo(0),
            $"PulsarBrokerEndpoint '{_name}': expected no messages, published {_published.Count}");

    public void AssertReceivedOnTopic(string topic, int expected) =>
        Assert.That(
            _published.Count(r => r.Topic == topic),
            Is.EqualTo(expected),
            $"PulsarBrokerEndpoint '{_name}': expected {expected} on '{topic}'");

    // ── Consumer-side assertions (messages received from real Pulsar) ────

    /// <summary>All messages consumed from real Pulsar subscriptions.</summary>
    public IReadOnlyList<ReceivedMessage> Consumed => _consumed.ToArray();

    /// <summary>Number of messages consumed from real Pulsar.</summary>
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
        Reset();
    }

    public sealed record ReceivedMessage(object Envelope, string Topic, DateTimeOffset ReceivedAt);
}
