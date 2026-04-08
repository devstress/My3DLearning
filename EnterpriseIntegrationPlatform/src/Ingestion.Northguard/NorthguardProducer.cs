using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Northguard;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to Northguard topics
/// via its HTTP/gRPC log storage API.
/// </summary>
/// <remarks>
/// <para>
/// Northguard is LinkedIn's next-generation log storage engine that replaces Apache Kafka
/// for ultra-high-scale workloads (32 trillion records/day, 17 PB+ across 150+ clusters).
/// It introduces fine-grained log striping — sharding data into segments and ranges instead
/// of monolithic partitions — and uses a Raft-backed, sharded metadata layer instead of
/// Kafka's single-controller model.
/// </para>
/// <para>
/// This implementation targets the Northguard REST/gRPC ingest API exposed through the
/// Xinfra virtualised pub/sub layer. Messages are serialised using the standard
/// <see cref="EnvelopeSerializer"/> and keyed by <see cref="IntegrationEnvelope{T}.CorrelationId"/>
/// for deterministic range-based routing.
/// </para>
/// <para>
/// <strong>Note:</strong> Northguard is currently an internal LinkedIn system. This
/// implementation provides the integration point for when the API becomes externally
/// available or when running inside LinkedIn's infrastructure.
/// </para>
/// </remarks>
public sealed class NorthguardProducer : IMessageBrokerProducer, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NorthguardProducer> _logger;

    /// <summary>Initialises a new <see cref="NorthguardProducer"/>.</summary>
    /// <param name="httpClient">
    /// Pre-configured <see cref="HttpClient"/> pointing at the Northguard ingest endpoint.
    /// </param>
    /// <param name="logger">Logger for diagnostics.</param>
    public NorthguardProducer(HttpClient httpClient, ILogger<NorthguardProducer> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        IntegrationEnvelope<T> envelope,
        string topic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var data = EnvelopeSerializer.Serialize(envelope);

        using var content = new ByteArrayContent(data);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Northguard-Topic", topic);
        content.Headers.Add("X-Northguard-Key", envelope.CorrelationId.ToString());
        content.Headers.Add("X-Northguard-MessageId", envelope.MessageId.ToString());

        var response = await _httpClient.PostAsync(
            $"/v1/topics/{Uri.EscapeDataString(topic)}/produce",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        _logger.LogDebug(
            "Published message {MessageId} to Northguard topic {Topic} with key {Key}",
            envelope.MessageId, topic, envelope.CorrelationId);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
