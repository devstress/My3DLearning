namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Thread-safe implementation of <see cref="IBackpressureSignal"/> using atomic operations.
/// Supports concurrent signal and release from multiple threads.
/// </summary>
public sealed class BackpressureSignal : IBackpressureSignal
{
    private int _signaled;

    /// <inheritdoc />
    public bool IsBackpressured => Volatile.Read(ref _signaled) == 1;

    /// <inheritdoc />
    public void Signal() => Interlocked.Exchange(ref _signaled, 1);

    /// <inheritdoc />
    public void Release() => Interlocked.Exchange(ref _signaled, 0);
}
