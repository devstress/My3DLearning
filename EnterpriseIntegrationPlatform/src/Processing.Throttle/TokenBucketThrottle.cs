using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Token-bucket message processing throttle. Tokens are replenished at
/// <see cref="ThrottleOptions.MaxMessagesPerSecond"/> per second up to
/// <see cref="ThrottleOptions.BurstCapacity"/>. When no token is available
/// the caller waits (backpressure) or is rejected depending on configuration.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Throttling ≠ Rate Limiting.</strong>
/// Rate limiting (Gateway.Api / Admin.Api) rejects excess HTTP requests with 429.
/// Throttling controls the <em>speed of message processing</em> by delaying the
/// consumer — smoothing throughput and preventing downstream overload.
/// </para>
/// </remarks>
public sealed class TokenBucketThrottle : IMessageThrottle, IDisposable
{
    private readonly ThrottleOptions _options;
    private readonly ILogger<TokenBucketThrottle> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _refillTimer;
    private readonly object _metricsLock = new();

    private long _totalAcquired;
    private long _totalRejected;
    private long _totalWaitTicks;

    /// <summary>
    /// Initializes a new <see cref="TokenBucketThrottle"/> with the configured
    /// burst capacity and refill rate.
    /// </summary>
    public TokenBucketThrottle(
        IOptions<ThrottleOptions> options,
        ILogger<TokenBucketThrottle> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        // SemaphoreSlim acts as the token bucket:
        // - initialCount  = burst capacity (tokens available at startup)
        // - maxCount       = burst capacity (cannot exceed bucket size)
        _semaphore = new SemaphoreSlim(
            initialCount: _options.BurstCapacity,
            maxCount: _options.BurstCapacity);

        // Refill interval: release tokens at the configured rate.
        // We refill once per 100 ms for smooth throughput.
        var refillIntervalMs = 100;
        var tokensPerInterval = Math.Max(1, _options.MaxMessagesPerSecond / (1000 / refillIntervalMs));

        _refillTimer = new Timer(
            callback: _ => Refill(tokensPerInterval),
            state: null,
            dueTime: refillIntervalMs,
            period: refillIntervalMs);
    }

    /// <inheritdoc />
    public int AvailableTokens => _semaphore.CurrentCount;

    /// <inheritdoc />
    public async Task<ThrottleResult> AcquireAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (_options.RejectOnBackpressure && _semaphore.CurrentCount == 0)
        {
            Interlocked.Increment(ref _totalRejected);

            _logger.LogWarning(
                "Throttle backpressure: rejecting message {MessageId} (0 tokens available)",
                envelope.MessageId);

            return new ThrottleResult
            {
                Permitted = false,
                WaitTime = TimeSpan.Zero,
                RemainingTokens = 0,
                RejectionReason = "Backpressure: no tokens available and RejectOnBackpressure is enabled.",
            };
        }

        var sw = Stopwatch.StartNew();

        bool acquired;
        try
        {
            acquired = await _semaphore.WaitAsync(_options.MaxWaitTime, ct);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Interlocked.Increment(ref _totalRejected);

            return new ThrottleResult
            {
                Permitted = false,
                WaitTime = sw.Elapsed,
                RemainingTokens = _semaphore.CurrentCount,
                RejectionReason = "Cancelled while waiting for throttle token.",
            };
        }

        sw.Stop();
        Interlocked.Add(ref _totalWaitTicks, sw.Elapsed.Ticks);

        if (!acquired)
        {
            Interlocked.Increment(ref _totalRejected);

            _logger.LogWarning(
                "Throttle timeout: message {MessageId} waited {WaitMs} ms without acquiring a token",
                envelope.MessageId,
                sw.ElapsedMilliseconds);

            return new ThrottleResult
            {
                Permitted = false,
                WaitTime = sw.Elapsed,
                RemainingTokens = _semaphore.CurrentCount,
                RejectionReason = $"Timeout: waited {sw.ElapsedMilliseconds} ms, max is {_options.MaxWaitTime.TotalMilliseconds} ms.",
            };
        }

        Interlocked.Increment(ref _totalAcquired);

        if (sw.ElapsedMilliseconds > 100)
        {
            _logger.LogDebug(
                "Throttle delayed message {MessageId} by {WaitMs} ms",
                envelope.MessageId,
                sw.ElapsedMilliseconds);
        }

        return new ThrottleResult
        {
            Permitted = true,
            WaitTime = sw.Elapsed,
            RemainingTokens = _semaphore.CurrentCount,
        };
    }

    /// <inheritdoc />
    public ThrottleMetrics GetMetrics()
    {
        return new ThrottleMetrics
        {
            TotalAcquired = Interlocked.Read(ref _totalAcquired),
            TotalRejected = Interlocked.Read(ref _totalRejected),
            AvailableTokens = _semaphore.CurrentCount,
            BurstCapacity = _options.BurstCapacity,
            RefillRate = _options.MaxMessagesPerSecond,
            TotalWaitTime = new TimeSpan(Interlocked.Read(ref _totalWaitTicks)),
        };
    }

    /// <summary>Releases tokens back into the bucket up to burst capacity.</summary>
    private void Refill(int tokensToAdd)
    {
        for (var i = 0; i < tokensToAdd; i++)
        {
            try
            {
                _semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Bucket is full — expected when processing is slower than refill.
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _refillTimer.Dispose();
        _semaphore.Dispose();
    }
}
