using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IConsumerScaler"/>
/// that manages a pool of logical consumer instances.
/// </summary>
public sealed class InMemoryConsumerScaler : IConsumerScaler
{
    private readonly ILogger<InMemoryConsumerScaler> _logger;
    private readonly object _lock = new();
    private int _currentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConsumerScaler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initialCount">The initial number of consumer instances.</param>
    public InMemoryConsumerScaler(ILogger<InMemoryConsumerScaler> logger, int initialCount = 1)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCount);

        _logger = logger;
        _currentCount = initialCount;
    }

    /// <inheritdoc />
    public int CurrentCount
    {
        get
        {
            lock (_lock)
            {
                return _currentCount;
            }
        }
    }

    /// <inheritdoc />
    public Task ScaleAsync(int desiredCount, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(desiredCount);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (desiredCount == _currentCount)
            {
                return Task.CompletedTask;
            }

            var previousCount = _currentCount;
            _currentCount = desiredCount;

            if (desiredCount > previousCount)
            {
                _logger.LogInformation(
                    "Scaled up consumer pool from {PreviousCount} to {DesiredCount}",
                    previousCount, desiredCount);
            }
            else
            {
                _logger.LogInformation(
                    "Scaled down consumer pool from {PreviousCount} to {DesiredCount}",
                    previousCount, desiredCount);
            }
        }

        return Task.CompletedTask;
    }
}
