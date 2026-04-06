// ============================================================================
// Tutorial 45 – Performance Profiling (Lab)
// ============================================================================
// This lab exercises ContinuousProfiler, AllocationHotspotDetector,
// InMemoryBenchmarkRegistry, ProfileSnapshot, OperationStats, and
// ProfilingOptions.
// ============================================================================

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Performance.Profiling;

namespace TutorialLabs.Tutorial45;

[TestFixture]
public sealed class Lab
{
    // ── ContinuousProfiler Captures Snapshot with Label ─────────────────────

    [Test]
    public void ContinuousProfiler_CaptureSnapshot_WithLabel()
    {
        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        var snapshot = profiler.CaptureSnapshot("baseline");

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Label, Is.EqualTo("baseline"));
        Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
        Assert.That(snapshot.Cpu, Is.Not.Null);
        Assert.That(snapshot.Memory, Is.Not.Null);
        Assert.That(snapshot.Gc, Is.Not.Null);
    }

    // ── ContinuousProfiler.SnapshotCount Increments ─────────────────────────

    [Test]
    public void ContinuousProfiler_SnapshotCount_Increments()
    {
        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        Assert.That(profiler.SnapshotCount, Is.EqualTo(0));

        profiler.CaptureSnapshot();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(1));

        profiler.CaptureSnapshot();
        profiler.CaptureSnapshot();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(3));
    }

    // ── ContinuousProfiler.GetLatestSnapshot Returns Last Captured ──────────

    [Test]
    public void ContinuousProfiler_GetLatestSnapshot_ReturnsLastCaptured()
    {
        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        Assert.That(profiler.GetLatestSnapshot(), Is.Null);

        profiler.CaptureSnapshot("first");
        profiler.CaptureSnapshot("second");
        var latest = profiler.CaptureSnapshot("third");

        var retrieved = profiler.GetLatestSnapshot();
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.SnapshotId, Is.EqualTo(latest.SnapshotId));
        Assert.That(retrieved.Label, Is.EqualTo("third"));
    }

    // ── AllocationHotspotDetector Registers and Retrieves Stats ─────────────

    [Test]
    public void AllocationHotspotDetector_RegisterAndGetOperationStats()
    {
        var detector = new AllocationHotspotDetector(
            NullLogger<AllocationHotspotDetector>.Instance,
            Options.Create(new ProfilingOptions()));

        detector.RegisterOperation("ProcessOrder", TimeSpan.FromMilliseconds(100), 1024);
        detector.RegisterOperation("ProcessOrder", TimeSpan.FromMilliseconds(200), 2048);

        var stats = detector.GetOperationStats("ProcessOrder");

        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.OperationName, Is.EqualTo("ProcessOrder"));
        Assert.That(stats.InvocationCount, Is.EqualTo(2));
        Assert.That(stats.AverageDuration, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
        Assert.That(stats.MaxDuration, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
        Assert.That(stats.MinDuration, Is.EqualTo(TimeSpan.FromMilliseconds(100)));
        Assert.That(stats.TotalAllocatedBytes, Is.EqualTo(3072));
    }

    // ── InMemoryBenchmarkRegistry Registers and Retrieves Baseline ──────────

    [Test]
    public void InMemoryBenchmarkRegistry_RegisterAndGetBaseline()
    {
        var registry = new InMemoryBenchmarkRegistry(
            NullLogger<InMemoryBenchmarkRegistry>.Instance);

        var baseline = new BenchmarkBaseline
        {
            BenchmarkName = "SerializeOrder",
            MeanDuration = TimeSpan.FromMilliseconds(5),
            MeanAllocatedBytes = 4096,
            Iterations = 1000,
            RecordedAt = DateTimeOffset.UtcNow,
        };

        registry.RegisterBaseline(baseline);

        var retrieved = registry.GetBaseline("SerializeOrder");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.BenchmarkName, Is.EqualTo("SerializeOrder"));
        Assert.That(retrieved.MeanDuration, Is.EqualTo(TimeSpan.FromMilliseconds(5)));
        Assert.That(retrieved.MeanAllocatedBytes, Is.EqualTo(4096));
        Assert.That(retrieved.Iterations, Is.EqualTo(1000));
        Assert.That(retrieved.RegressionThresholdPercent, Is.EqualTo(20.0));
    }

    // ── ProfilingOptions Defaults ───────────────────────────────────────────

    [Test]
    public void ProfilingOptions_Defaults()
    {
        var opts = new ProfilingOptions();

        Assert.That(opts.Enabled, Is.True);
        Assert.That(opts.MaxRetainedSnapshots, Is.EqualTo(1000));
        Assert.That(opts.SnapshotInterval, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(opts.MaxTrackedOperations, Is.EqualTo(10000));
        Assert.That(opts.HotspotThresholds, Is.Not.Null);
    }

    // ── ProfileSnapshot Record Shape ────────────────────────────────────────

    [Test]
    public void ProfileSnapshot_RecordShape()
    {
        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        var snapshot = profiler.CaptureSnapshot("shape-test");

        Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
        Assert.That(snapshot.CapturedAt, Is.GreaterThan(DateTimeOffset.MinValue));
        Assert.That(snapshot.Cpu, Is.Not.Null);
        Assert.That(snapshot.Cpu.ThreadCount, Is.GreaterThan(0));
        Assert.That(snapshot.Memory, Is.Not.Null);
        Assert.That(snapshot.Memory.WorkingSetBytes, Is.GreaterThan(0));
        Assert.That(snapshot.Gc, Is.Not.Null);
        Assert.That(snapshot.Label, Is.EqualTo("shape-test"));
    }
}
