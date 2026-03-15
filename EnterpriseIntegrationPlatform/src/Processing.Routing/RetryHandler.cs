namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Retry Handler — automatically retries failed operations with configurable
/// delay and maximum attempts. Equivalent to BizTalk Send Port retry settings
/// and failed message routing with retry logic.
/// </summary>
public interface IRetryHandler
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}

/// <summary>
/// Configuration for retry behaviour.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Maximum number of attempts (including the first).</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Delay between attempts.</summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Multiplier applied to delay after each retry (exponential back-off).</summary>
    public double BackoffMultiplier { get; init; } = 2.0;
}

/// <summary>
/// Retry handler with configurable exponential back-off.
/// </summary>
public sealed class RetryHandler : IRetryHandler
{
    private readonly RetryOptions _options;

    public RetryHandler(RetryOptions? options = null) =>
        _options = options ?? new RetryOptions();

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        var delay = _options.Delay;

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch (Exception) when (attempt < _options.MaxAttempts)
            {
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * _options.BackoffMultiplier);
            }
        }
    }
}
