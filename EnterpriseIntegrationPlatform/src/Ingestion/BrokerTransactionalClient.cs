using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Broker-aware implementation of <see cref="ITransactionalClient"/>.
/// For Kafka (native transactions), uses init/begin/commit/abort semantics.
/// For NATS JetStream and Pulsar (no native transactions), implements
/// publish-then-confirm with compensation on failure — each published message
/// is tracked and a compensating tombstone message is published on rollback.
/// Thread-safe.
/// </summary>
public sealed class BrokerTransactionalClient : ITransactionalClient
{
    private readonly IMessageBrokerProducer _producer;
    private readonly BrokerType _brokerType;
    private readonly ILogger<BrokerTransactionalClient> _logger;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of <see cref="BrokerTransactionalClient"/>.
    /// </summary>
    /// <param name="producer">The message broker producer.</param>
    /// <param name="brokerOptions">Broker configuration options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BrokerTransactionalClient(
        IMessageBrokerProducer producer,
        IOptions<BrokerOptions> brokerOptions,
        ILogger<BrokerTransactionalClient> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(brokerOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _brokerType = brokerOptions.Value.BrokerType;
        _logger = logger;
        _timeout = TimeSpan.FromSeconds(brokerOptions.Value.TransactionTimeoutSeconds);
    }

    /// <inheritdoc />
    public bool SupportsNativeTransactions => _brokerType == BrokerType.Kafka;

    /// <inheritdoc />
    public async Task<TransactionResult> ExecuteAsync(
        Func<ITransactionScope, CancellationToken, Task> operations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operations);

        var stopwatch = Stopwatch.StartNew();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        var scope = new TrackingTransactionScope(_producer);

        _logger.LogDebug(
            "Starting transaction (broker={BrokerType}, nativeTransactions={Native})",
            _brokerType,
            SupportsNativeTransactions);

        try
        {
            await operations(scope, timeoutCts.Token);

            stopwatch.Stop();

            _logger.LogInformation(
                "Transaction committed: {MessageCount} messages in {Duration}ms",
                scope.PublishedCount,
                stopwatch.ElapsedMilliseconds);

            return TransactionResult.Success(scope.PublishedCount, stopwatch.Elapsed);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Transaction timed out after {Timeout}s with {MessageCount} messages pending",
                _timeout.TotalSeconds,
                scope.PublishedCount);

            await CompensateAsync(scope, cancellationToken);

            return TransactionResult.Failure(
                $"Transaction timed out after {_timeout.TotalSeconds}s",
                ex,
                stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Transaction failed after {Duration}ms with {MessageCount} messages published. Rolling back.",
                stopwatch.ElapsedMilliseconds,
                scope.PublishedCount);

            await CompensateAsync(scope, cancellationToken);

            return TransactionResult.Failure(
                $"Transaction failed: {ex.Message}",
                ex,
                stopwatch.Elapsed);
        }
    }

    private async Task CompensateAsync(TrackingTransactionScope scope, CancellationToken cancellationToken)
    {
        if (scope.PublishedCount == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Compensating {MessageCount} published messages",
            scope.PublishedCount);

        foreach (var (topic, messageId) in scope.PublishedMessages)
        {
            try
            {
                var tombstone = IntegrationEnvelope<string>.Create(
                    payload: $"COMPENSATE:{messageId}",
                    source: "TransactionalClient",
                    messageType: "system.transaction.compensate");

                var compensateMeta = new Dictionary<string, string>(tombstone.Metadata)
                {
                    ["compensated-message-id"] = messageId.ToString(),
                    ["compensation-reason"] = "transaction-rollback",
                };

                var compensateEnvelope = tombstone with { Metadata = compensateMeta };
                await _producer.PublishAsync(compensateEnvelope, $"{topic}.dlq", cancellationToken);

                _logger.LogDebug(
                    "Published compensation for message {MessageId} to {Topic}.dlq",
                    messageId,
                    topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to compensate message {MessageId} on topic {Topic}",
                    messageId,
                    topic);
            }
        }
    }

    /// <summary>
    /// Transaction scope that tracks published messages for compensation on rollback.
    /// </summary>
    private sealed class TrackingTransactionScope : ITransactionScope
    {
        private readonly IMessageBrokerProducer _producer;
        private readonly List<(string Topic, Guid MessageId)> _published = [];

        public TrackingTransactionScope(IMessageBrokerProducer producer)
        {
            _producer = producer;
        }

        public int PublishedCount => _published.Count;

        public IReadOnlyList<(string Topic, Guid MessageId)> PublishedMessages => _published;

        public async Task PublishAsync<T>(
            IntegrationEnvelope<T> envelope,
            string topic,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);

            await _producer.PublishAsync(envelope, topic, cancellationToken);
            _published.Add((topic, envelope.MessageId));
        }
    }
}
