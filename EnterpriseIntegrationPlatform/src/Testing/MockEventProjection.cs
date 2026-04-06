// ============================================================================
// MockEventProjection – Configurable event projection for testing
// ============================================================================

using EnterpriseIntegrationPlatform.EventSourcing;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IEventProjection{TState}"/>
/// that applies a configurable projection function.
/// </summary>
public sealed class MockEventProjection<TState> : IEventProjection<TState>
{
    private readonly Func<TState, EventEnvelope, TState> _projectFunc;
    private int _callCount;

    /// <summary>Creates a mock projection with the given function.</summary>
    public MockEventProjection(Func<TState, EventEnvelope, TState> projectFunc) =>
        _projectFunc = projectFunc;

    /// <summary>Number of projection calls.</summary>
    public int CallCount => _callCount;

    public Task<TState> ProjectAsync(TState state, EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult(_projectFunc(state, envelope));
    }
}
