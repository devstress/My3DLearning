namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// An <see cref="IPayloadTransform{TIn,TOut}"/> backed by a caller-supplied delegate.
/// Use this when the transformation logic is defined as a simple lambda or method reference
/// rather than a dedicated class.
/// </summary>
/// <typeparam name="TIn">Source payload type.</typeparam>
/// <typeparam name="TOut">Target payload type.</typeparam>
public sealed class FuncPayloadTransform<TIn, TOut> : IPayloadTransform<TIn, TOut>
{
    private readonly Func<TIn, TOut> _transform;

    /// <summary>
    /// Initialises a new instance of <see cref="FuncPayloadTransform{TIn,TOut}"/> with
    /// the supplied <paramref name="transform"/> delegate.
    /// </summary>
    /// <param name="transform">The delegate that performs the payload transformation.</param>
    public FuncPayloadTransform(Func<TIn, TOut> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        _transform = transform;
    }

    /// <inheritdoc />
    public TOut Transform(TIn source) => _transform(source);
}
