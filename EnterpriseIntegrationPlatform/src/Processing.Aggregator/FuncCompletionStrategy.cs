using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Aggregator;

/// <summary>
/// An <see cref="ICompletionStrategy{T}"/> backed by a caller-supplied predicate delegate.
/// Use for inline or lambda-based completion logic.
/// </summary>
/// <typeparam name="T">The payload type of the individual messages.</typeparam>
public sealed class FuncCompletionStrategy<T> : ICompletionStrategy<T>
{
    private readonly Func<IReadOnlyList<IntegrationEnvelope<T>>, bool> _predicate;

    /// <summary>
    /// Initialises a new instance of <see cref="FuncCompletionStrategy{T}"/>.
    /// </summary>
    /// <param name="predicate">
    /// Delegate that returns <see langword="true"/> when the group is ready
    /// to be aggregated.
    /// </param>
    public FuncCompletionStrategy(Func<IReadOnlyList<IntegrationEnvelope<T>>, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicate = predicate;
    }

    /// <inheritdoc />
    public bool IsComplete(IReadOnlyList<IntegrationEnvelope<T>> group) => _predicate(group);
}
