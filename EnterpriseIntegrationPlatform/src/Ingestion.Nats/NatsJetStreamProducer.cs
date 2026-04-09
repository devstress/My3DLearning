using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace EnterpriseIntegrationPlatform.Ingestion.Nats;

/// <summary>
/// Publishes <see cref="IntegrationEnvelope{T}"/> messages to NATS JetStream subjects.
/// Each subject acts as an independent channel, avoiding Head-of-Line blocking
/// between recipients.
/// </summary>
public sealed class NatsJetStreamProducer : IMessageBrokerProducer, IAsyncDisposable
{
    internal static readonly ActivitySource ActivitySource = new("EIP.Ingestion.Nats.Producer");

    private readonly INatsConnection _connection;
    private readonly INatsJSContext _js;
    private readonly ILogger<NatsJetStreamProducer> _logger;
    private readonly NatsOptions _options;
    private bool _disposed;

    /// <summary>Initialises a new <see cref="NatsJetStreamProducer"/>.</summary>
    public NatsJetStreamProducer(
        INatsConnection connection,
        ILogger<NatsJetStreamProducer> logger,
        IOptions<NatsOptions> options)
    {
        _connection = connection;
        _js = new NatsJSContext((NatsConnection)connection);
        _logger = logger;
        _options = options.Value;
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

        using var activity = ActivitySource.StartActivity("NatsJetStream.Publish");
        activity?.SetTag("messaging.system", "nats");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.message_id", envelope.MessageId.ToString());

        var data = EnvelopeSerializer.Serialize(envelope);

        await EnsureStreamAsync(topic, cancellationToken);

        await _js.PublishAsync(
            subject: topic,
            data: data,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published message {MessageId} to NATS subject {Subject}",
            envelope.MessageId, topic);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_connection is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    private async Task EnsureStreamAsync(string topic, CancellationToken ct)
    {
        var streamName = topic.Replace(".", "-");
        var maxRetries = _options.MaxRetries;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _js.GetStreamAsync(streamName, cancellationToken: ct);
                return;
            }
            catch (NatsJSApiException ex) when (ex.Error.Code == 404)
            {
                try
                {
                    await _js.CreateStreamAsync(
                        new StreamConfig(streamName, [topic]),
                        ct);
                    return;
                }
                catch (NatsJSApiNoResponseException) when (attempt < maxRetries)
                {
                    _logger.LogWarning(
                        "JetStream create-stream timeout (attempt {Attempt}/{Max}), retrying…",
                        attempt, maxRetries);
                    await Task.Delay(_options.RetryDelayMs * attempt, ct);
                }
                catch (NatsJSApiNoResponseException) when (attempt >= maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Failed to create NATS JetStream stream '{streamName}' after {maxRetries} attempts.");
                }
            }
            catch (NatsJSApiNoResponseException) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "JetStream get-stream timeout (attempt {Attempt}/{Max}), retrying…",
                    attempt, maxRetries);
                await Task.Delay(_options.RetryDelayMs * attempt, ct);
            }
            catch (NatsJSApiNoResponseException) when (attempt >= maxRetries)
            {
                throw new InvalidOperationException(
                    $"Failed to verify NATS JetStream stream '{streamName}' after {maxRetries} attempts.");
            }
        }
    }
}
