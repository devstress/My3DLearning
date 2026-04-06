// ============================================================================
// Tutorial 45 – Performance Profiling (Exam)
// ============================================================================
// Coding challenges: hotspot detection, benchmark regression detection,
// and profiler time-range query.
// ============================================================================

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Performance.Profiling;

namespace TutorialLabs.Tutorial45;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Hotspot Detection ──────────────────────────────────────

    [Test]
    public void Challenge1_HotspotDetection_RegisterOperationsDetectAboveThreshold()
    {
        var detector = new AllocationHotspotDetector(
            NullLogger<AllocationHotspotDetector>.Instance,
            Options.Create(new ProfilingOptions()));

        // Register a slow operation (above warning threshold of 500ms)
        for (var i = 0; i < 10; i++)
        {
            detector.RegisterOperation("SlowQuery", TimeSpan.FromMilliseconds(800), 2_000_000);
        }

        // Register a fast operation (below thresholds)
        for (var i = 0; i < 10; i++)
        {
            detector.RegisterOperation("FastQuery", TimeSpan.FromMilliseconds(10), 1024);
        }

        var thresholds = new HotspotThresholds
        {
            DurationWarningMs = 500,
            DurationCriticalMs = 2000,
            AllocationWarningBytes = 1_048_576,
            AllocationCriticalBytes = 10_485_760,
            MinimumInvocations = 5,
        };

        var hotspots = detector.DetectHotspots(thresholds);

        // SlowQuery should be flagged for both duration and allocation
        var slowQueryHotspots = hotspots.Where(h => h.OperationName == "SlowQuery").ToList();
        Assert.That(slowQueryHotspots, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(slowQueryHotspots.Any(h => h.Category == "Duration"), Is.True);
        Assert.That(slowQueryHotspots.Any(h => h.Category == "Allocation"), Is.True);

        // FastQuery should not be flagged
        var fastQueryHotspots = hotspots.Where(h => h.OperationName == "FastQuery").ToList();
        Assert.That(fastQueryHotspots, Is.Empty);
    }

    // ── Challenge 2: Benchmark Regression Detection ─────────────────────────

    [Test]
    public void Challenge2_BenchmarkRegressionDetection()
    {
        var registry = new InMemoryBenchmarkRegistry(
            NullLogger<InMemoryBenchmarkRegistry>.Instance);

        var baseline = new BenchmarkBaseline
        {
            BenchmarkName = "OrderPipeline",
            MeanDuration = TimeSpan.FromMilliseconds(100),
            MeanAllocatedBytes = 10_000,
            Iterations = 500,
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-7),
            RegressionThresholdPercent = 20.0,
        };

        registry.RegisterBaseline(baseline);

        // Worse result: 50% slower and 50% more allocations
        var worseResult = new BenchmarkResult
        {
            BenchmarkName = "OrderPipeline",
            MeanDuration = TimeSpan.FromMilliseconds(150),
            MeanAllocatedBytes = 15_000,
            Iterations = 500,
            RunAt = DateTimeOffset.UtcNow,
        };

        var regression = registry.Compare(worseResult);

        Assert.That(regression, Is.Not.Null);
        Assert.That(regression!.HasRegression, Is.True);
        Assert.That(regression.DurationRegressed, Is.True);
        Assert.That(regression.AllocationRegressed, Is.True);
        Assert.That(regression.DurationChangePercent, Is.GreaterThan(20.0));
        Assert.That(regression.AllocationChangePercent, Is.GreaterThan(20.0));
        Assert.That(regression.Baseline.BenchmarkName, Is.EqualTo("OrderPipeline"));
        Assert.That(regression.Current.MeanDuration, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
    }

    // ── Challenge 3: Profiler Time-Range Query ──────────────────────────────

    [Test]
    public void Challenge3_ProfilerTimeRangeQuery()
    {
        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        var beforeCapture = DateTimeOffset.UtcNow.AddSeconds(-1);

        var snap1 = profiler.CaptureSnapshot("snap-1");
        var snap2 = profiler.CaptureSnapshot("snap-2");
        var snap3 = profiler.CaptureSnapshot("snap-3");

        var afterCapture = DateTimeOffset.UtcNow.AddSeconds(1);

        // Query all snapshots within our time range
        var snapshots = profiler.GetSnapshots(beforeCapture, afterCapture);

        Assert.That(snapshots, Has.Count.EqualTo(3));
        Assert.That(snapshots[0].Label, Is.EqualTo("snap-1"));
        Assert.That(snapshots[1].Label, Is.EqualTo("snap-2"));
        Assert.That(snapshots[2].Label, Is.EqualTo("snap-3"));

        // Verify ordering is by CapturedAt ascending
        Assert.That(snapshots[0].CapturedAt, Is.LessThanOrEqualTo(snapshots[1].CapturedAt));
        Assert.That(snapshots[1].CapturedAt, Is.LessThanOrEqualTo(snapshots[2].CapturedAt));

        // Query with a narrow range should exclude snapshots outside it
        var narrowRange = profiler.GetSnapshots(
            DateTimeOffset.UtcNow.AddMinutes(1),
            DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.That(narrowRange, Is.Empty);
    }
}
