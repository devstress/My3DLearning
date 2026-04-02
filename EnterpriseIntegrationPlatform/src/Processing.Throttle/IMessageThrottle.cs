using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Controls the rate at which messages are <em>processed</em> using a
/// token-bucket algorithm. Unlike HTTP rate limiting (which rejects with 429),
/// throttling <strong>delays</strong> message processing to smooth throughput.
/// </summary>
public interface IMessageThrottle
{
    /// <summary>
    /// Acquires a processing token for the given envelope. The call blocks
    /// (asynchronously) until a token is available, the configured
    /// <see cref="ThrottleOptions.MaxWaitTime"/> elapses, or the
    /// <paramref name="ct"/> is cancelled.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message requesting processing capacity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ThrottleResult"/> indicating whether processing may proceed.
    /// </returns>
    Task<ThrottleResult> AcquireAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the current number of available tokens in the bucket.
    /// Useful for diagnostics and backpressure signaling.
    /// </summary>
    int AvailableTokens { get; }

    /// <summary>
    /// Returns current throttle metrics for observability.
    /// </summary>
    ThrottleMetrics GetMetrics();
}
