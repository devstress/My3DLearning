using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Performance.Profiling;

/// <summary>
/// Thread-safe in-memory benchmark registry that stores baselines and compares
/// benchmark results against them to detect performance regressions.
/// </summary>
public sealed class InMemoryBenchmarkRegistry : IBenchmarkRegistry
{
    private readonly ILogger<InMemoryBenchmarkRegistry> _logger;
    private readonly ConcurrentDictionary<string, BenchmarkBaseline> _baselines = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryBenchmarkRegistry"/>.
    /// </summary>
    public InMemoryBenchmarkRegistry(ILogger<InMemoryBenchmarkRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterBaseline(BenchmarkBaseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseline.BenchmarkName);

        _baselines[baseline.BenchmarkName] = baseline;

        _logger.LogInformation(
            "Registered benchmark baseline {Name}: MeanDuration={Duration}ms, MeanAllocation={Alloc}bytes, Iterations={Iter}",
            baseline.BenchmarkName,
            baseline.MeanDuration.TotalMilliseconds,
            baseline.MeanAllocatedBytes,
            baseline.Iterations);
    }

    /// <inheritdoc />
    public BenchmarkBaseline? GetBaseline(string benchmarkName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(benchmarkName);
        return _baselines.GetValueOrDefault(benchmarkName);
    }

    /// <inheritdoc />
    public BenchmarkRegression? Compare(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(result.BenchmarkName);

        if (!_baselines.TryGetValue(result.BenchmarkName, out var baseline))
        {
            _logger.LogDebug("No baseline found for benchmark {Name}", result.BenchmarkName);
            return null;
        }

        var durationChangePercent = baseline.MeanDuration.TotalMilliseconds > 0
            ? ((result.MeanDuration.TotalMilliseconds - baseline.MeanDuration.TotalMilliseconds) / baseline.MeanDuration.TotalMilliseconds) * 100.0
            : 0.0;

        var allocationChangePercent = baseline.MeanAllocatedBytes > 0
            ? ((double)(result.MeanAllocatedBytes - baseline.MeanAllocatedBytes) / baseline.MeanAllocatedBytes) * 100.0
            : 0.0;

        var durationRegressed = durationChangePercent > baseline.RegressionThresholdPercent;
        var allocationRegressed = allocationChangePercent > baseline.RegressionThresholdPercent;

        var regression = new BenchmarkRegression
        {
            BenchmarkName = result.BenchmarkName,
            DurationRegressed = durationRegressed,
            AllocationRegressed = allocationRegressed,
            DurationChangePercent = durationChangePercent,
            AllocationChangePercent = allocationChangePercent,
            Baseline = baseline,
            Current = result
        };

        if (regression.HasRegression)
        {
            _logger.LogWarning(
                "Benchmark regression detected in {Name}: Duration={DurationChange:+0.0;-0.0}%, Allocation={AllocChange:+0.0;-0.0}%",
                result.BenchmarkName,
                durationChangePercent,
                allocationChangePercent);
        }
        else
        {
            _logger.LogDebug(
                "Benchmark {Name} within baseline: Duration={DurationChange:+0.0;-0.0}%, Allocation={AllocChange:+0.0;-0.0}%",
                result.BenchmarkName,
                durationChangePercent,
                allocationChangePercent);
        }

        return regression;
    }

    /// <inheritdoc />
    public IReadOnlyList<BenchmarkBaseline> GetAllBaselines()
    {
        return _baselines.Values.OrderBy(b => b.BenchmarkName).ToList();
    }

    /// <inheritdoc />
    public bool RemoveBaseline(string benchmarkName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(benchmarkName);
        var removed = _baselines.TryRemove(benchmarkName, out _);

        if (removed)
        {
            _logger.LogInformation("Removed benchmark baseline {Name}", benchmarkName);
        }

        return removed;
    }
}
