// ============================================================================
// MockPayloadTransform – Configurable payload transform for testing
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Translator;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IPayloadTransform{TIn,TOut}"/>
/// that applies a configurable transform function.
/// </summary>
public sealed class MockPayloadTransform<TIn, TOut> : IPayloadTransform<TIn, TOut>
{
    private readonly Func<TIn, TOut> _transformFunc;
    private readonly List<TIn> _inputs = new();

    /// <summary>Creates a mock transform with the given function.</summary>
    public MockPayloadTransform(Func<TIn, TOut> transformFunc) =>
        _transformFunc = transformFunc;

    /// <summary>All inputs that were transformed.</summary>
    public IReadOnlyList<TIn> Inputs => _inputs;

    /// <summary>Number of transforms performed.</summary>
    public int TransformCount => _inputs.Count;

    public TOut Transform(TIn source)
    {
        _inputs.Add(source);
        return _transformFunc(source);
    }
}
