using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Background service that periodically monitors consumer lag and makes scaling decisions.
/// Applies backpressure when lag exceeds the scale-up threshold and the consumer pool
/// is already at maximum capacity. Cooldown periods prevent scaling flapping.
/// </summary>
public sealed class CompetingConsumerOrchestrator : BackgroundService
{
    private readonly IConsumerLagMonitor _lagMonitor;
    private readonly IConsumerScaler _scaler;
    private readonly IBackpressureSignal _backpressure;
    private readonly CompetingConsumerOptions _options;
    private readonly ILogger<CompetingConsumerOrchestrator> _logger;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastScaleTime = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompetingConsumerOrchestrator"/> class.
    /// </summary>
    /// <param name="lagMonitor">The consumer lag monitor.</param>
    /// <param name="scaler">The consumer scaler.</param>
    /// <param name="backpressure">The backpressure signal.</param>
    /// <param name="options">The competing consumer options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for cooldown tracking.</param>
    public CompetingConsumerOrchestrator(
        IConsumerLagMonitor lagMonitor,
        IConsumerScaler scaler,
        IBackpressureSignal backpressure,
        IOptions<CompetingConsumerOptions> options,
        ILogger<CompetingConsumerOrchestrator> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(lagMonitor);
        ArgumentNullException.ThrowIfNull(scaler);
        ArgumentNullException.ThrowIfNull(backpressure);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _lagMonitor = lagMonitor;
        _scaler = scaler;
        _backpressure = backpressure;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CompetingConsumerOrchestrator starting — topic '{Topic}', group '{Group}', " +
            "min {Min}, max {Max}",
            _options.TargetTopic, _options.ConsumerGroup,
            _options.MinConsumers, _options.MaxConsumers);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAndScaleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during competing consumer orchestration cycle");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.CooldownMs), _timeProvider, stoppingToken);
        }

        _logger.LogInformation("CompetingConsumerOrchestrator stopped");
    }

    internal async Task EvaluateAndScaleAsync(CancellationToken cancellationToken)
    {
        var lagInfo = await _lagMonitor.GetLagAsync(
            _options.TargetTopic, _options.ConsumerGroup, cancellationToken);

        var currentCount = _scaler.CurrentCount;
        var now = _timeProvider.GetUtcNow();
        var elapsed = now - _lastScaleTime;
        var cooldown = TimeSpan.FromMilliseconds(_options.CooldownMs);

        if (lagInfo.CurrentLag >= _options.ScaleUpThreshold)
        {
            if (currentCount >= _options.MaxConsumers)
            {
                _backpressure.Signal();
                _logger.LogWarning(
                    "Consumer pool at maximum ({Max}), lag {Lag} — backpressure signaled",
                    _options.MaxConsumers, lagInfo.CurrentLag);
                return;
            }

            _backpressure.Release();

            if (elapsed < cooldown)
            {
                _logger.LogDebug(
                    "Scale-up deferred — cooldown has {Remaining}ms remaining",
                    (cooldown - elapsed).TotalMilliseconds);
                return;
            }

            var desired = Math.Min(currentCount + 1, _options.MaxConsumers);
            await _scaler.ScaleAsync(desired, cancellationToken);
            _lastScaleTime = now;

            _logger.LogInformation(
                "Scaled up consumers from {Current} to {Desired} (lag: {Lag})",
                currentCount, desired, lagInfo.CurrentLag);
        }
        else if (lagInfo.CurrentLag <= _options.ScaleDownThreshold)
        {
            _backpressure.Release();

            if (currentCount <= _options.MinConsumers)
            {
                return;
            }

            if (elapsed < cooldown)
            {
                _logger.LogDebug(
                    "Scale-down deferred — cooldown has {Remaining}ms remaining",
                    (cooldown - elapsed).TotalMilliseconds);
                return;
            }

            var desired = Math.Max(currentCount - 1, _options.MinConsumers);
            await _scaler.ScaleAsync(desired, cancellationToken);
            _lastScaleTime = now;

            _logger.LogInformation(
                "Scaled down consumers from {Current} to {Desired} (lag: {Lag})",
                currentCount, desired, lagInfo.CurrentLag);
        }
        else
        {
            _backpressure.Release();
        }
    }
}
