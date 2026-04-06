// ============================================================================
// NatsBrokerEndpoint – Real NATS-backed endpoint with MockEndpoint assertions
// ============================================================================
// Wraps real NatsJetStreamProducer and NatsJetStreamConsumer with the same
// assertion API as MockEndpoint, so tutorials can assert message counts,
// topics, and payloads after real broker round-trips.
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Nats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NUnit.Framework;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Real NATS JetStream-backed message endpoint that provides the same
/// assertion API as <see cref="EnterpriseIntegrationPlatform.Testing.MockEndpoint"/>.
/// <para>
/// On the <b>producer</b> side it publishes to real NATS subjects.
/// On the <b>consumer</b> side it subscribes and captures received messages
/// for test assertions.
/// </para>
/// </summary>
public sealed class NatsBrokerEndpoint : IMessageBrokerProducer, IMessageBrokerConsumer,
    IEventDrivenConsumer, IPollingConsumer, ISelectiveConsumer, IAsyncDisposable
{
    private readonly string _name;
    private readonly NatsConnection _connection;
    private readonly NatsJetStreamProducer _producer;
    private readonly ConcurrentQueue<ReceivedMessage> _received = new();
    private readonly ConcurrentQueue<object> _inbound = new();
    private readonly List<Func<object, Task>> _handlers = new();
    private readonly List<CancellationTokenSource> _subscriptionTokens = new();
    private readonly INatsJSContext _js;

    public NatsBrokerEndpoint(string name, string natsUrl)
    {
        _name = name;
        _connection = new NatsConnection(new NatsOpts { Url = natsUrl });
        _js = new NatsJSContext(_connection);
        _producer = new NatsJetStreamProducer(
            _connection,
            NullLogger<NatsJetStreamProducer>.Instance);
    }

    public string Name => _name;

    // ── IMessageBrokerProducer (publishes to real NATS) ─────────────────

    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        await _producer.PublishAsync(envelope, topic, cancellationToken);
        // Also capture locally for assertions
        _received.Enqueue(new ReceivedMessage(envelope!, topic, DateTimeOffset.UtcNow));
    }

    // ── IMessageBrokerConsumer (subscribes on real NATS) ────────────────

    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _subscriptionTokens.Add(cts);

        var streamName = topic.Replace(".", "-");
        await EnsureStreamAsync(streamName, topic, cts.Token);

        var consumer = await _js.CreateOrUpdateConsumerAsync(
            streamName,
            new ConsumerConfig(consumerGroup + "-" + Guid.NewGuid().ToString("N")[..8])
            {
                FilterSubject = topic,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
            },
            cts.Token);

        // Run consumption in background
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cts.Token))
                {
                    if (msg.Data is null)
                    {
                        await msg.AckAsync(cancellationToken: cts.Token);
                        continue;
                    }

                    var env = EnvelopeSerializer.Deserialize<T>(msg.Data);
                    if (env is not null)
                    {
                        _received.Enqueue(new ReceivedMessage(env!, topic, DateTimeOffset.UtcNow));
                        _inbound.Enqueue(env!);
                        await handler(env);
                    }
                    await msg.AckAsync(cancellationToken: cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }, cts.Token);
    }

    // ── IEventDrivenConsumer ────────────────────────────────────────────

    public Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default) =>
        SubscribeAsync(topic, consumerGroup, handler, cancellationToken);

    // ── IPollingConsumer ────────────────────────────────────────────────

    public Task<IReadOnlyList<IntegrationEnvelope<T>>> PollAsync<T>(
        string topic,
        string consumerGroup,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IntegrationEnvelope<T>>();
        while (results.Count < maxMessages && _inbound.TryDequeue(out var msg))
            results.Add((IntegrationEnvelope<T>)msg);
        return Task.FromResult<IReadOnlyList<IntegrationEnvelope<T>>>(results);
    }

    // ── ISelectiveConsumer ──────────────────────────────────────────────

    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        await SubscribeAsync<T>(
            topic,
            consumerGroup,
            async env =>
            {
                if (predicate(env))
                    await handler(env);
            },
            cancellationToken);
    }

    // ── Test helpers: send messages (publishes to real NATS) ────────────

    /// <summary>
    /// Sends a test message through real NATS, triggering any registered subscribers.
    /// </summary>
    public async Task SendAsync<T>(IntegrationEnvelope<T> envelope, string topic = "test-input")
    {
        await _producer.PublishAsync(envelope, topic, CancellationToken.None);
    }

    // ── Assertions (same API as MockEndpoint) ───────────────────────────

    public IReadOnlyList<ReceivedMessage> Received => _received.ToArray();

    public int ReceivedCount => _received.Count;

    public IntegrationEnvelope<T> GetReceived<T>(int index = 0) =>
        (IntegrationEnvelope<T>)_received.ElementAt(index).Envelope;

    public IReadOnlyList<IntegrationEnvelope<T>> GetAllReceived<T>(string? topic = null) =>
        _received
            .Where(r => topic is null || r.Topic == topic)
            .Select(r => (IntegrationEnvelope<T>)r.Envelope)
            .ToList();

    public IReadOnlyList<string> GetReceivedTopics() =>
        _received.Select(r => r.Topic).Distinct().ToList();

    public void AssertReceivedCount(int expected) =>
        Assert.That(_received.Count, Is.EqualTo(expected),
            $"NatsBrokerEndpoint '{_name}': expected {expected} message(s), received {_received.Count}");

    public void AssertNoneReceived() =>
        Assert.That(_received.Count, Is.EqualTo(0),
            $"NatsBrokerEndpoint '{_name}': expected no messages, received {_received.Count}");

    public void AssertReceivedOnTopic(string topic, int expected) =>
        Assert.That(
            _received.Count(r => r.Topic == topic),
            Is.EqualTo(expected),
            $"NatsBrokerEndpoint '{_name}': expected {expected} on '{topic}'");

    /// <summary>
    /// Polls until the expected message count is reached or timeout expires.
    /// Essential for real broker tests where delivery is asynchronous.
    /// </summary>
    public async Task WaitForMessagesAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (_received.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Polls until the expected message count on a specific topic is reached.
    /// </summary>
    public async Task WaitForMessagesOnTopicAsync(string topic, int expectedCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (_received.Count(r => r.Topic == topic) < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    public void Reset()
    {
        while (_received.TryDequeue(out _)) { }
        while (_inbound.TryDequeue(out _)) { }
        _handlers.Clear();
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
        await _connection.DisposeAsync();
    }

    public sealed record ReceivedMessage(object Envelope, string Topic, DateTimeOffset ReceivedAt);

    // ── Private helpers ─────────────────────────────────────────────────

    private async Task EnsureStreamAsync(string streamName, string topic, CancellationToken ct)
    {
        try
        {
            await _js.GetStreamAsync(streamName, cancellationToken: ct);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            await _js.CreateStreamAsync(
                new StreamConfig(streamName, [topic]),
                ct);
        }
    }
}
