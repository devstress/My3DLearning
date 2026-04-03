using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace UnitTests.ProfilingTests;

[TestFixture]
public class ContinuousProfilerTests
{
    private ILogger<ContinuousProfiler> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<ContinuousProfiler>>();
    }

    private ContinuousProfiler BuildProfiler(ProfilingOptions? options = null)
    {
        options ??= new ProfilingOptions();
        return new ContinuousProfiler(_logger, Options.Create(options));
    }

    [Test]
    public void CaptureSnapshot_ReturnsSnapshotWithUniqueId()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void CaptureSnapshot_SetsTimestamp()
    {
        var profiler = BuildProfiler();
        var before = DateTimeOffset.UtcNow;

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.CapturedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(snapshot.CapturedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    [Test]
    public void CaptureSnapshot_CapturesCpuMetrics()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Cpu, Is.Not.Null);
        Assert.That(snapshot.Cpu.ThreadCount, Is.GreaterThan(0));
        Assert.That(snapshot.Cpu.TotalProcessorTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
    }

    [Test]
    public void CaptureSnapshot_CapturesMemoryMetrics()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Memory, Is.Not.Null);
        Assert.That(snapshot.Memory.WorkingSetBytes, Is.GreaterThan(0));
        Assert.That(snapshot.Memory.ManagedHeapSizeBytes, Is.GreaterThan(0));
    }

    [Test]
    public void CaptureSnapshot_CapturesGcMetrics()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Gc, Is.Not.Null);
        Assert.That(snapshot.Gc.Gen0Collections, Is.GreaterThanOrEqualTo(0));
        Assert.That(snapshot.Gc.Gen1Collections, Is.GreaterThanOrEqualTo(0));
        Assert.That(snapshot.Gc.Gen2Collections, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void CaptureSnapshot_FirstSnapshot_CpuPercentIsNull()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Cpu.CpuUsagePercent, Is.Null);
    }

    [Test]
    public void CaptureSnapshot_SecondSnapshot_CpuPercentIsNotNull()
    {
        var profiler = BuildProfiler();
        profiler.CaptureSnapshot();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Cpu.CpuUsagePercent, Is.Not.Null);
    }

    [Test]
    public void CaptureSnapshot_FirstSnapshot_AllocationRateIsNull()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Memory.AllocationRateBytesPerSecond, Is.Null);
    }

    [Test]
    public void CaptureSnapshot_SecondSnapshot_AllocationRateIsNotNull()
    {
        var profiler = BuildProfiler();
        profiler.CaptureSnapshot();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Memory.AllocationRateBytesPerSecond, Is.Not.Null);
    }

    [Test]
    public void CaptureSnapshot_WithLabel_SetsLabel()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot("baseline");

        Assert.That(snapshot.Label, Is.EqualTo("baseline"));
    }

    [Test]
    public void CaptureSnapshot_WithoutLabel_LabelIsNull()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Label, Is.Null);
    }

    [Test]
    public void SnapshotCount_IncrementsAfterCapture()
    {
        var profiler = BuildProfiler();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(0));

        profiler.CaptureSnapshot();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(1));

        profiler.CaptureSnapshot();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(2));
    }

    [Test]
    public void CaptureSnapshot_ExceedsMaxRetained_EvictsOldest()
    {
        var options = new ProfilingOptions { MaxRetainedSnapshots = 3 };
        var profiler = BuildProfiler(options);

        profiler.CaptureSnapshot("first");
        profiler.CaptureSnapshot("second");
        profiler.CaptureSnapshot("third");
        profiler.CaptureSnapshot("fourth");

        Assert.That(profiler.SnapshotCount, Is.EqualTo(3));

        var all = profiler.GetSnapshots(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        Assert.That(all.Any(s => s.Label == "first"), Is.False);
        Assert.That(all.Any(s => s.Label == "fourth"), Is.True);
    }

    [Test]
    public void GetLatestSnapshot_NoSnapshots_ReturnsNull()
    {
        var profiler = BuildProfiler();

        Assert.That(profiler.GetLatestSnapshot(), Is.Null);
    }

    [Test]
    public void GetLatestSnapshot_ReturnsLastCaptured()
    {
        var profiler = BuildProfiler();
        profiler.CaptureSnapshot("first");
        profiler.CaptureSnapshot("last");

        var latest = profiler.GetLatestSnapshot();

        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Label, Is.EqualTo("last"));
    }

    [Test]
    public void GetSnapshots_FiltersOnTimeRange()
    {
        var profiler = BuildProfiler();

        var s1 = profiler.CaptureSnapshot("s1");
        Thread.Sleep(10);
        var boundary = DateTimeOffset.UtcNow;
        Thread.Sleep(10);
        var s2 = profiler.CaptureSnapshot("s2");

        var result = profiler.GetSnapshots(boundary, DateTimeOffset.MaxValue);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Label, Is.EqualTo("s2"));
    }

    [Test]
    public void CaptureSnapshot_MultipleSnapshots_AllHaveUniqueIds()
    {
        var profiler = BuildProfiler();

        var ids = Enumerable.Range(0, 10)
            .Select(_ => profiler.CaptureSnapshot().SnapshotId)
            .ToList();

        Assert.That(ids.Distinct().Count(), Is.EqualTo(10));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(() => new ContinuousProfiler(null!, Options.Create(new ProfilingOptions())),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.That(() => new ContinuousProfiler(_logger, null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void CaptureSnapshot_GcMetrics_HasValidFragmentationRatio()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Gc.FragmentationRatio, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(snapshot.Gc.FragmentationRatio, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void CaptureSnapshot_GcMetrics_HasValidPauseTimePercentage()
    {
        var profiler = BuildProfiler();

        var snapshot = profiler.CaptureSnapshot();

        Assert.That(snapshot.Gc.PauseTimePercentage, Is.GreaterThanOrEqualTo(0.0));
    }
}
