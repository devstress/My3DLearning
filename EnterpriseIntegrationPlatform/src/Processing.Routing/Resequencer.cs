using System.Collections.Concurrent;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Resequencer — reorders messages that arrive out of order.
/// Buffers messages until a complete, ordered sequence is available.
/// Equivalent to BizTalk Sequential Convoy with ordered delivery.
/// </summary>
public interface IResequencer<T>
{
    /// <summary>Adds a message with its sequence number.</summary>
    void Add(IntegrationEnvelope<T> envelope, long sequenceNumber);

    /// <summary>
    /// Retrieves all messages that are ready to be released in order,
    /// starting from the next expected sequence number.
    /// </summary>
    IReadOnlyList<IntegrationEnvelope<T>> ReleaseInOrder();
}

/// <summary>
/// In-memory resequencer that buffers and releases messages in order.
/// </summary>
public sealed class Resequencer<T> : IResequencer<T>
{
    private readonly SortedDictionary<long, IntegrationEnvelope<T>> _buffer = new();
    private long _nextExpected;

    public Resequencer(long startSequence = 0)
    {
        _nextExpected = startSequence;
    }

    /// <summary>Next expected sequence number.</summary>
    public long NextExpected => _nextExpected;

    /// <inheritdoc />
    public void Add(IntegrationEnvelope<T> envelope, long sequenceNumber)
    {
        _buffer[sequenceNumber] = envelope;
    }

    /// <inheritdoc />
    public IReadOnlyList<IntegrationEnvelope<T>> ReleaseInOrder()
    {
        var result = new List<IntegrationEnvelope<T>>();

        while (_buffer.ContainsKey(_nextExpected))
        {
            result.Add(_buffer[_nextExpected]);
            _buffer.Remove(_nextExpected);
            _nextExpected++;
        }

        return result;
    }
}
