using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Resequencer;

/// <summary>
/// Production implementation of the Resequencer EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Buffers out-of-order messages keyed by <c>CorrelationId</c> and indexed by
/// <c>SequenceNumber</c>. When all messages in a sequence have arrived (determined
/// by <c>TotalCount</c>), they are released in order. Incomplete sequences are
/// released when <see cref="ReleaseOnTimeout{T}"/> is called (typically by a timer).
/// </para>
/// <para>
/// Thread-safe: uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for storage.
/// Duplicate sequence numbers for the same correlation ID are ignored.
/// </para>
/// </remarks>
public sealed class MessageResequencer : IResequencer
{
    private readonly ConcurrentDictionary<Guid, SequenceBuffer> _buffers = new();
    private readonly ResequencerOptions _options;
    private readonly ILogger<MessageResequencer> _logger;

    /// <summary>Initialises a new instance of <see cref="MessageResequencer"/>.</summary>
    public MessageResequencer(
        IOptions<ResequencerOptions> options,
        ILogger<MessageResequencer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public int ActiveSequenceCount => _buffers.Count;

    /// <inheritdoc />
    public IReadOnlyList<IntegrationEnvelope<T>> Accept<T>(IntegrationEnvelope<T> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.SequenceNumber is null || envelope.TotalCount is null)
        {
            throw new ArgumentException(
                $"Message {envelope.MessageId} must have SequenceNumber and TotalCount set.",
                nameof(envelope));
        }

        if (envelope.TotalCount.Value <= 0)
        {
            throw new ArgumentException(
                $"TotalCount must be positive, got {envelope.TotalCount.Value}.",
                nameof(envelope));
        }

        var correlationId = envelope.CorrelationId;
        var seqNum = envelope.SequenceNumber.Value;
        var totalCount = envelope.TotalCount.Value;

        var buffer = _buffers.GetOrAdd(correlationId, _ =>
        {
            if (_buffers.Count >= _options.MaxConcurrentSequences)
            {
                _logger.LogWarning(
                    "Resequencer at capacity ({Max} sequences). Message {MessageId} may be dropped.",
                    _options.MaxConcurrentSequences, envelope.MessageId);
            }

            return new SequenceBuffer(totalCount);
        });

        // Duplicate check
        if (buffer.Has(seqNum))
        {
            _logger.LogDebug(
                "Duplicate sequence {Seq} for correlation {CorrelationId} — ignored",
                seqNum, correlationId);
            return [];
        }

        buffer.Add(seqNum, envelope);

        _logger.LogDebug(
            "Buffered message {MessageId} seq={Seq}/{Total} for correlation {CorrelationId}",
            envelope.MessageId, seqNum, totalCount, correlationId);

        // Check if sequence is complete
        if (buffer.IsComplete)
        {
            _buffers.TryRemove(correlationId, out _);

            var ordered = buffer.GetOrdered<T>();

            _logger.LogInformation(
                "Sequence complete for correlation {CorrelationId} — releasing {Count} messages in order",
                correlationId, ordered.Count);

            return ordered;
        }

        return [];
    }

    /// <inheritdoc />
    public IReadOnlyList<IntegrationEnvelope<T>> ReleaseOnTimeout<T>(Guid correlationId)
    {
        if (!_buffers.TryRemove(correlationId, out var buffer))
            return [];

        var ordered = buffer.GetOrdered<T>();

        _logger.LogInformation(
            "Timeout release for correlation {CorrelationId} — releasing {Count}/{Total} messages",
            correlationId, ordered.Count, buffer.ExpectedCount);

        return ordered;
    }

    /// <summary>
    /// Internal buffer tracking messages for a single sequence (one CorrelationId).
    /// </summary>
    private sealed class SequenceBuffer
    {
        private readonly ConcurrentDictionary<int, object> _messages = new();

        public int ExpectedCount { get; }

        public SequenceBuffer(int expectedCount)
        {
            ExpectedCount = expectedCount;
        }

        public bool Has(int sequenceNumber) => _messages.ContainsKey(sequenceNumber);

        public void Add(int sequenceNumber, object envelope) =>
            _messages.TryAdd(sequenceNumber, envelope);

        public bool IsComplete => _messages.Count >= ExpectedCount;

        public IReadOnlyList<IntegrationEnvelope<T>> GetOrdered<T>() =>
            _messages
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => (IntegrationEnvelope<T>)kvp.Value)
                .ToList()
                .AsReadOnly();
    }
}
