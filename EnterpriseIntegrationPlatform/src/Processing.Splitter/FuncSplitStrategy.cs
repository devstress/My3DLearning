namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// An <see cref="ISplitStrategy{T}"/> backed by a caller-supplied delegate.
/// Use this when the split logic is defined as a simple lambda or method reference
/// rather than a dedicated class.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed class FuncSplitStrategy<T> : ISplitStrategy<T>
{
    private readonly Func<T, IReadOnlyList<T>> _splitFunc;

    /// <summary>
    /// Initialises a new instance of <see cref="FuncSplitStrategy{T}"/> with
    /// the supplied <paramref name="splitFunc"/> delegate.
    /// </summary>
    /// <param name="splitFunc">The delegate that performs the payload split.</param>
    public FuncSplitStrategy(Func<T, IReadOnlyList<T>> splitFunc)
    {
        ArgumentNullException.ThrowIfNull(splitFunc);
        _splitFunc = splitFunc;
    }

    /// <inheritdoc />
    public IReadOnlyList<T> Split(T composite) => _splitFunc(composite);
}
