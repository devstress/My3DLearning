using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// An <see cref="ICompletionStrategy{T}"/> that considers a group complete when the
/// number of received envelopes reaches or exceeds a fixed expected count.
/// </summary>
/// <typeparam name="T">The payload type of the individual messages.</typeparam>
public sealed class CountCompletionStrategy<T> : ICompletionStrategy<T>
{
    private readonly int _expectedCount;

    /// <summary>
    /// Initialises a new instance of <see cref="CountCompletionStrategy{T}"/>.
    /// </summary>
    /// <param name="expectedCount">
    /// The number of envelopes that must be received before the group is considered
    /// complete. Must be greater than zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount"/> is less than or equal to zero.
    /// </exception>
    public CountCompletionStrategy(int expectedCount)
    {
        if (expectedCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedCount),
                expectedCount,
                "Expected count must be greater than zero.");

        _expectedCount = expectedCount;
    }

    /// <inheritdoc />
    public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) =>
        group.Count >= _expectedCount;
}
