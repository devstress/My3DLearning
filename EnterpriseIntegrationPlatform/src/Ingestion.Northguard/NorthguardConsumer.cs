using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Ingestion.Northguard;

/// <summary>
/// Consumes <see cref="IntegrationEnvelope{T}"/> messages from Northguard topics
/// via its HTTP/gRPC consume API.
/// </summary>
/// <remarks>
/// <para>
/// Northguard consumers use a long-poll HTTP endpoint (or gRPC streaming) to fetch
/// segments of messages from the log storage layer. Unlike Kafka's partition-based
/// model, Northguard uses fine-grained ranges that are automatically split, merged,
/// and rebalanced across consumers without stop-the-world operations.
/// </para>
/// <para>
/// Consumer groups are managed server-side by the Northguard coordinator. Each
/// consumer in a group is assigned a set of ranges; when consumers join or leave,
/// ranges are seamlessly reassigned with no data loss or duplication.
/// </para>
/// <para>
/// <strong>Note:</strong> Northguard is currently an internal LinkedIn system. This
/// implementation provides the integration point for when the API becomes externally
/// available or when running inside LinkedIn's infrastructure.
/// </para>
/// </remarks>
public sealed class NorthguardConsumer : IMessageBrokerConsumer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NorthguardConsumer> _logger;

    /// <summary>Initialises a new <see cref="NorthguardConsumer"/>.</summary>
    /// <param name="httpClientFactory">
    /// Factory for creating <see cref="HttpClient"/> instances configured for the Northguard endpoint.
    /// </param>
    /// <param name="logger">Logger for diagnostics.</param>
    public NorthguardConsumer(IHttpClientFactory httpClientFactory, ILogger<NorthguardConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(
        string topic,
        string consumerGroup,
        Func<IntegrationEnvelope<T>, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Subscribed to Northguard topic {Topic} with consumer group {Group}",
            topic, consumerGroup);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient(
                    NorthguardServiceExtensions.HttpClientName);

                var requestUri = $"/v1/topics/{Uri.EscapeDataString(topic)}/consume" +
                                 $"?group={Uri.EscapeDataString(consumerGroup)}&timeout=30";

                var response = await client.GetAsync(requestUri, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Northguard consume returned {StatusCode} for topic {Topic}",
                        response.StatusCode, topic);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (data.Length == 0)
                {
                    continue;
                }

                var envelope = EnvelopeSerializer.Deserialize<T>(data);
                if (envelope is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise message on Northguard topic {Topic}", topic);
                    continue;
                }

                await handler(envelope);

                // Acknowledge the message
                using var ackClient = _httpClientFactory.CreateClient(
                    NorthguardServiceExtensions.HttpClientName);
                using var ackContent = new StringContent(
                    JsonSerializer.Serialize(new { messageId = envelope.MessageId }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                await ackClient.PostAsync(
                    $"/v1/topics/{Uri.EscapeDataString(topic)}/ack" +
                    $"?group={Uri.EscapeDataString(consumerGroup)}",
                    ackContent,
                    cancellationToken);

                _logger.LogDebug(
                    "Processed message {MessageId} from Northguard topic {Topic}",
                    envelope.MessageId, topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error consuming from Northguard topic {Topic}", topic);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
