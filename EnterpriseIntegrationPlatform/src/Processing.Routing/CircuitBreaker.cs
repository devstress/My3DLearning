namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Circuit Breaker — prevents repeated calls to a failing service by
/// opening the circuit after a threshold of failures. Equivalent to
/// BizTalk adapter retry + service window patterns that stop sending
/// to unavailable endpoints.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>Current state of the circuit.</summary>
    CircuitState State { get; }

    /// <summary>Executes an operation through the circuit breaker.</summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);

    /// <summary>Manually resets the circuit to closed.</summary>
    void Reset();
}

/// <summary>Circuit breaker states.</summary>
public enum CircuitState
{
    /// <summary>Normal operation — requests pass through.</summary>
    Closed,

    /// <summary>Circuit is open — requests are rejected immediately.</summary>
    Open,

    /// <summary>Trial state — a single request is allowed to test recovery.</summary>
    HalfOpen,
}

/// <summary>
/// In-memory circuit breaker with failure threshold and recovery timeout.
/// </summary>
public sealed class CircuitBreaker : ICircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private int _failureCount;
    private DateTimeOffset _openedAt;
    private readonly object _lock = new();

    public CircuitBreaker(int failureThreshold = 3, TimeSpan? openDuration = null)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_failureCount < _failureThreshold) return CircuitState.Closed;
                if (DateTimeOffset.UtcNow - _openedAt >= _openDuration) return CircuitState.HalfOpen;
                return CircuitState.Open;
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        var state = State;
        if (state == CircuitState.Open)
            throw new CircuitBreakerOpenException("Circuit breaker is open.");

        try
        {
            var result = await operation(ct);
            lock (_lock) _failureCount = 0; // Success — reset
            return result;
        }
        catch (Exception)
        {
            lock (_lock)
            {
                _failureCount++;
                if (_failureCount >= _failureThreshold)
                    _openedAt = DateTimeOffset.UtcNow;
            }
            throw;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
        }
    }
}

/// <summary>Thrown when a circuit breaker is in the Open state.</summary>
public sealed class CircuitBreakerOpenException : InvalidOperationException
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
