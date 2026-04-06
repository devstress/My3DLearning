// ============================================================================
// MockEndpoint – Real in-memory message broker for end-to-end integration testing
// ============================================================================
// Captures messages published by EIP components and feeds test messages to
// consumers. Inspired by Apache Camel's MockEndpoint pattern. A real service
// implementation — not a test double.
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory message broker endpoint for end-to-end integration testing.
/// Implements all broker interfaces so it can act as both producer (captures
/// outbound messages) and consumer (feeds inbound messages to handlers).
/// </summary>
public sealed class MockEndpoint : IMessageBrokerProducer, IMessageBrokerConsumer,
    IEventDrivenConsumer, IPollingConsumer, ISelectiveConsumer, IAsyncDisposable
{
    private readonly string _name;
    private readonly ConcurrentQueue<ReceivedMessage> _received = new();
    private readonly ConcurrentQueue<object> _inbound = new();
    private readonly List<Func<object, Task>> _handlers = new();

    public MockEndpoint(string name) => _name = name;

    public string Name => _name;

    // ── IMessageBrokerProducer (captures outbound messages) ─────────────

    public Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        _received.Enqueue(new ReceivedMessage(envelope!, topic, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    // ── IMessageBrokerConsumer (delivers test messages to handlers) ──────

    public Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        _handlers.Add(obj => handler((IntegrationEnvelope<T>)obj));
        return Task.CompletedTask;
    }

    // ── IEventDrivenConsumer ────────────────────────────────────────────

    public Task StartAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        _handlers.Add(obj => handler((IntegrationEnvelope<T>)obj));
        return Task.CompletedTask;
    }

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

    public Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, bool> predicate,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        _handlers.Add(obj =>
        {
            var env = (IntegrationEnvelope<T>)obj;
            return predicate(env) ? handler(env) : Task.CompletedTask;
        });
        return Task.CompletedTask;
    }

    // ── Test helpers: Send messages into the endpoint ────────────────────

    /// <summary>
    /// Sends a test message into this endpoint, triggering registered handlers.
    /// Use this to feed messages into the integration pipeline under test.
    /// </summary>
    public async Task SendAsync<T>(IntegrationEnvelope<T> envelope)
    {
        _inbound.Enqueue(envelope!);
        foreach (var handler in _handlers)
            await handler(envelope!);
    }

    // ── Assertions ──────────────────────────────────────────────────────

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
            $"MockEndpoint '{_name}': expected {expected} message(s), received {_received.Count}");

    public void AssertNoneReceived() =>
        Assert.That(_received.Count, Is.EqualTo(0),
            $"MockEndpoint '{_name}': expected no messages, received {_received.Count}");

    public void AssertReceivedOnTopic(string topic, int expected) =>
        Assert.That(
            _received.Count(r => r.Topic == topic),
            Is.EqualTo(expected),
            $"MockEndpoint '{_name}': expected {expected} on '{topic}'");

    public void Reset()
    {
        while (_received.TryDequeue(out _)) { }
        while (_inbound.TryDequeue(out _)) { }
        _handlers.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Reset();
        return ValueTask.CompletedTask;
    }

    public sealed record ReceivedMessage(object Envelope, string Topic, DateTimeOffset ReceivedAt);
}
