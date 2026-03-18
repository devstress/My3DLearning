using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Retry;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;
    private readonly Random _random;

    public ExponentialBackoffRetryPolicy(
        IOptions<RetryOptions> options,
        ILogger<ExponentialBackoffRetryPolicy> logger)
    {
        _options = options.Value;
        _logger = logger;
        _random = new Random();
    }

    public async Task<RetryResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _logger.LogDebug("Retry attempt {Attempt} of {MaxAttempts}", attempt, _options.MaxAttempts);
                var result = await operation(ct);
                return new RetryResult<T> { IsSucceeded = true, Attempts = attempt, Result = result };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Attempt {Attempt} failed. {MaxAttempts} max attempts.", attempt, _options.MaxAttempts);
                if (attempt < _options.MaxAttempts)
                {
                    var delay = ComputeDelay(attempt);
                    if (delay > 0)
                        await Task.Delay(delay, ct);
                }
            }
        }
        return new RetryResult<T> { IsSucceeded = false, Attempts = _options.MaxAttempts, LastException = lastException };
    }

    public async Task<RetryResult<bool>> ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct)
    {
        var result = await ExecuteAsync<bool>(async innerCt =>
        {
            await operation(innerCt);
            return true;
        }, ct);
        return result;
    }

    private int ComputeDelay(int attempt)
    {
        var exponential = _options.InitialDelayMs * Math.Pow(_options.BackoffMultiplier, attempt - 1);
        var capped = Math.Min(exponential, _options.MaxDelayMs);
        if (!_options.UseJitter)
            return (int)capped;
        var jitter = capped * 0.2 * (2.0 * _random.NextDouble() - 1.0);
        return Math.Max(0, (int)(capped + jitter));
    }
}
