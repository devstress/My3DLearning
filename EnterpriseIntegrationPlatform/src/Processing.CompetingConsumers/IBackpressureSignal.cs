namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Provides a mechanism for signaling and releasing backpressure
/// to coordinate producer/consumer throughput.
/// </summary>
public interface IBackpressureSignal
{
    /// <summary>
    /// Gets a value indicating whether backpressure is currently active.
    /// </summary>
    bool IsBackpressured { get; }

    /// <summary>
    /// Activates the backpressure signal.
    /// </summary>
    void Signal();

    /// <summary>
    /// Releases the backpressure signal.
    /// </summary>
    void Release();
}
