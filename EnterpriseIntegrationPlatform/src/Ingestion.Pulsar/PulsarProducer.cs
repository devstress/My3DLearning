using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Pulsar;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to Apache Pulsar topics.
/// Each message is keyed by <see cref="IntegrationEnvelope{T}.CorrelationId"/> so that
/// Key_Shared consumers can distribute messages by recipient without HOL blocking.
/// </summary>
/// <remarks>
/// Producer instances are cached per topic to avoid the overhead of creating a new
/// producer for every publish. Cached producers are disposed when the
/// <see cref="PulsarProducer"/> is disposed.
/// </remarks>
public sealed class PulsarProducer : IMessageBrokerProducer, IAsyncDisposable
{
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Pulsar.Producer");

    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarProducer> _logger;
    private readonly ConcurrentDictionary<string, IProducer<ReadOnlySequence<byte>>> _producers = new();
    private bool _disposed;

    /// <summary>Initialises a new <see cref="PulsarProducer"/>.</summary>
    public PulsarProducer(IPulsarClient client, ILogger<PulsarProducer> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        using var activity = ActivitySource.StartActivity("Pulsar.Publish");
        activity?.SetTag("messaging.system", "pulsar");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

        var data = EnvelopeSerializer.Serialize(envelope);

        var producer = _producers.GetOrAdd(topic, t =>
            _client.NewProducer()
                .Topic(t)
                .Create());

        var metadata = new MessageMetadata
        {
            Key = envelope.CorrelationId.ToString(),
        };

        await producer.Send(metadata, data, cancellationToken);

        _logger.LogDebug(
            "Published message {MessageId} to Pulsar topic {Topic} with key {Key}",
            envelope.MessageId, topic, envelope.CorrelationId);
    }

    /// <summary>Gets the number of cached producer instances (for diagnostics and testing).</summary>
    public int CachedProducerCount => _producers.Count;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _producers)
        {
            try
            {
                await kvp.Value.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error disposing Pulsar producer for topic {Topic}", kvp.Key);
            }
        }

        _producers.Clear();
    }
}
