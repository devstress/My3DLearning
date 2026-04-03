using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Performance.Profiling;

/// <summary>
/// Thread-safe hotspot detector that tracks operation metrics and identifies
/// CPU and memory hotspots based on configurable thresholds.
/// Uses lock-free concurrent data structures for high-throughput tracking.
/// </summary>
public sealed class AllocationHotspotDetector : IHotspotDetector
{
    private readonly ILogger<AllocationHotspotDetector> _logger;
    private readonly ProfilingOptions _options;
    private readonly ConcurrentDictionary<string, OperationAccumulator> _operations = new();

    /// <summary>
    /// Initializes a new instance of <see cref="AllocationHotspotDetector"/>.
    /// </summary>
    public AllocationHotspotDetector(
        ILogger<AllocationHotspotDetector> logger,
        IOptions<ProfilingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public void RegisterOperation(string operationName, TimeSpan duration, long allocatedBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentOutOfRangeException.ThrowIfNegative(allocatedBytes);

        if (_operations.Count >= _options.MaxTrackedOperations &&
            !_operations.ContainsKey(operationName))
        {
            _logger.LogWarning(
                "Maximum tracked operations ({Max}) reached; ignoring new operation {OperationName}",
                _options.MaxTrackedOperations,
                operationName);
            return;
        }

        var accumulator = _operations.GetOrAdd(operationName, _ => new OperationAccumulator());
        accumulator.Record(duration, allocatedBytes);
    }

    /// <inheritdoc />
    public IReadOnlyList<HotspotInfo> DetectHotspots(HotspotThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        var hotspots = new List<HotspotInfo>();

        foreach (var (name, accumulator) in _operations)
        {
            var stats = accumulator.ToStats(name);

            if (stats.InvocationCount < thresholds.MinimumInvocations)
                continue;

            // Duration hotspots
            var avgDurationMs = stats.AverageDuration.TotalMilliseconds;

            if (avgDurationMs >= thresholds.DurationCriticalMs)
            {
                hotspots.Add(new HotspotInfo
                {
                    OperationName = name,
                    Category = "Duration",
                    Severity = HotspotSeverity.Critical,
                    Description = $"Average duration {avgDurationMs:F1}ms exceeds critical threshold {thresholds.DurationCriticalMs:F0}ms",
                    MeasuredValue = avgDurationMs,
                    ThresholdValue = thresholds.DurationCriticalMs
                });
            }
            else if (avgDurationMs >= thresholds.DurationWarningMs)
            {
                hotspots.Add(new HotspotInfo
                {
                    OperationName = name,
                    Category = "Duration",
                    Severity = HotspotSeverity.Warning,
                    Description = $"Average duration {avgDurationMs:F1}ms exceeds warning threshold {thresholds.DurationWarningMs:F0}ms",
                    MeasuredValue = avgDurationMs,
                    ThresholdValue = thresholds.DurationWarningMs
                });
            }

            // Allocation hotspots
            var avgAllocBytes = stats.AverageAllocatedBytes;

            if (avgAllocBytes >= thresholds.AllocationCriticalBytes)
            {
                hotspots.Add(new HotspotInfo
                {
                    OperationName = name,
                    Category = "Allocation",
                    Severity = HotspotSeverity.Critical,
                    Description = $"Average allocation {avgAllocBytes / (1024.0 * 1024.0):F2}MB exceeds critical threshold {thresholds.AllocationCriticalBytes / (1024.0 * 1024.0):F0}MB",
                    MeasuredValue = avgAllocBytes,
                    ThresholdValue = thresholds.AllocationCriticalBytes
                });
            }
            else if (avgAllocBytes >= thresholds.AllocationWarningBytes)
            {
                hotspots.Add(new HotspotInfo
                {
                    OperationName = name,
                    Category = "Allocation",
                    Severity = HotspotSeverity.Warning,
                    Description = $"Average allocation {avgAllocBytes / (1024.0 * 1024.0):F2}MB exceeds warning threshold {thresholds.AllocationWarningBytes / (1024.0 * 1024.0):F0}MB",
                    MeasuredValue = avgAllocBytes,
                    ThresholdValue = thresholds.AllocationWarningBytes
                });
            }
        }

        if (hotspots.Count > 0)
        {
            _logger.LogWarning(
                "Detected {HotspotCount} performance hotspots across {OperationCount} operations",
                hotspots.Count,
                hotspots.Select(h => h.OperationName).Distinct().Count());
        }

        return hotspots;
    }

    /// <inheritdoc />
    public OperationStats? GetOperationStats(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return _operations.TryGetValue(operationName, out var accumulator)
            ? accumulator.ToStats(operationName)
            : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<OperationStats> GetAllOperationStats()
    {
        return _operations
            .Select(kvp => kvp.Value.ToStats(kvp.Key))
            .OrderByDescending(s => s.AverageDuration)
            .ToList();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _operations.Clear();
        _logger.LogInformation("Hotspot detector reset — all tracked operations cleared");
    }

    /// <summary>
    /// Thread-safe accumulator for operation metrics using interlocked operations.
    /// </summary>
    private sealed class OperationAccumulator
    {
        private long _invocationCount;
        private long _totalDurationTicks;
        private long _maxDurationTicks;
        private long _minDurationTicks = long.MaxValue;
        private long _totalAllocatedBytes;

        public void Record(TimeSpan duration, long allocatedBytes)
        {
            var ticks = duration.Ticks;

            Interlocked.Increment(ref _invocationCount);
            Interlocked.Add(ref _totalDurationTicks, ticks);
            Interlocked.Add(ref _totalAllocatedBytes, allocatedBytes);

            // Thread-safe max update
            long currentMax;
            do
            {
                currentMax = Interlocked.Read(ref _maxDurationTicks);
                if (ticks <= currentMax)
                    break;
            } while (Interlocked.CompareExchange(ref _maxDurationTicks, ticks, currentMax) != currentMax);

            // Thread-safe min update
            long currentMin;
            do
            {
                currentMin = Interlocked.Read(ref _minDurationTicks);
                if (ticks >= currentMin)
                    break;
            } while (Interlocked.CompareExchange(ref _minDurationTicks, ticks, currentMin) != currentMin);
        }

        public OperationStats ToStats(string operationName)
        {
            var count = Interlocked.Read(ref _invocationCount);
            var totalTicks = Interlocked.Read(ref _totalDurationTicks);
            var totalAlloc = Interlocked.Read(ref _totalAllocatedBytes);
            var maxTicks = Interlocked.Read(ref _maxDurationTicks);
            var minTicks = Interlocked.Read(ref _minDurationTicks);

            if (count == 0)
            {
                return new OperationStats
                {
                    OperationName = operationName,
                    InvocationCount = 0,
                    AverageDuration = TimeSpan.Zero,
                    MaxDuration = TimeSpan.Zero,
                    MinDuration = TimeSpan.Zero,
                    AverageAllocatedBytes = 0,
                    TotalAllocatedBytes = 0
                };
            }

            return new OperationStats
            {
                OperationName = operationName,
                InvocationCount = count,
                AverageDuration = TimeSpan.FromTicks(totalTicks / count),
                MaxDuration = TimeSpan.FromTicks(maxTicks),
                MinDuration = minTicks == long.MaxValue ? TimeSpan.Zero : TimeSpan.FromTicks(minTicks),
                AverageAllocatedBytes = totalAlloc / count,
                TotalAllocatedBytes = totalAlloc
            };
        }
    }
}
