using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace LoadTests;

/// <summary>
/// Benchmark regression load tests that validate profiling system performance
/// under concurrent load. Thresholds are intentionally generous (10× production)
/// to avoid CI flakiness — goal is catching catastrophic regressions.
/// </summary>
[TestFixture]
public class ProfilingLoadTests
{
    [Test]
    public void HotspotDetector_1000ConcurrentRegistrations_CompletesWithin5Seconds()
    {
        var logger = Substitute.For<ILogger<AllocationHotspotDetector>>();
        var options = Options.Create(new ProfilingOptions { MaxTrackedOperations = 100_000 });
        var detector = new AllocationHotspotDetector(logger, options);

        const int concurrentOps = 1000;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentOps)
            .Select(i => Task.Run(() =>
            {
                detector.RegisterOperation(
                    $"Operation-{i % 50}",
                    TimeSpan.FromMilliseconds(i % 100),
                    i * 100L);
            }))
            .ToArray();

        Task.WaitAll(tasks);
        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)),
            $"1000 concurrent hotspot registrations took {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public void HotspotDetector_DetectHotspotsWithManyOperations_CompletesWithin2Seconds()
    {
        var logger = Substitute.For<ILogger<AllocationHotspotDetector>>();
        var options = Options.Create(new ProfilingOptions { MaxTrackedOperations = 10_000 });
        var detector = new AllocationHotspotDetector(logger, options);

        // Register 5000 operations with 10 invocations each
        for (var op = 0; op < 5000; op++)
        {
            for (var inv = 0; inv < 10; inv++)
            {
                detector.RegisterOperation($"Op-{op}", TimeSpan.FromMilliseconds(op % 1000), op * 100L);
            }
        }

        var sw = Stopwatch.StartNew();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 5,
            DurationWarningMs = 500,
            AllocationWarningBytes = 100_000
        };

        var hotspots = detector.DetectHotspots(thresholds);
        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)),
            $"Hotspot detection across 5000 operations took {sw.ElapsedMilliseconds}ms");
        Assert.That(hotspots, Is.Not.Empty);
    }

    [Test]
    public void BenchmarkRegistry_500ConcurrentCompares_CompletesWithin3Seconds()
    {
        var logger = Substitute.For<ILogger<InMemoryBenchmarkRegistry>>();
        var registry = new InMemoryBenchmarkRegistry(logger);

        // Register 50 baselines
        for (var i = 0; i < 50; i++)
        {
            registry.RegisterBaseline(new BenchmarkBaseline
            {
                BenchmarkName = $"Benchmark-{i}",
                MeanDuration = TimeSpan.FromMilliseconds(100 + i),
                MeanAllocatedBytes = 1000 * (i + 1),
                Iterations = 100,
                RecordedAt = DateTimeOffset.UtcNow,
                RegressionThresholdPercent = 20.0
            });
        }

        const int concurrentCompares = 500;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentCompares)
            .Select(i => Task.Run(() =>
            {
                var result = new BenchmarkResult
                {
                    BenchmarkName = $"Benchmark-{i % 50}",
                    MeanDuration = TimeSpan.FromMilliseconds(100 + (i % 50) + (i % 30)),
                    MeanAllocatedBytes = 1000 * ((i % 50) + 1) + (i * 10),
                    Iterations = 100,
                    RunAt = DateTimeOffset.UtcNow
                };
                return registry.Compare(result);
            }))
            .ToArray();

        Task.WaitAll(tasks);
        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)),
            $"500 concurrent benchmark comparisons took {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public void ContinuousProfiler_200RapidSnapshots_CompletesWithin5Seconds()
    {
        var logger = Substitute.For<ILogger<ContinuousProfiler>>();
        var options = Options.Create(new ProfilingOptions { MaxRetainedSnapshots = 200 });
        var profiler = new ContinuousProfiler(logger, options);

        const int snapshotCount = 200;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < snapshotCount; i++)
        {
            profiler.CaptureSnapshot($"snap-{i}");
        }

        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)),
            $"200 rapid profile snapshots took {sw.ElapsedMilliseconds}ms");
        Assert.That(profiler.SnapshotCount, Is.EqualTo(snapshotCount));
    }

    [Test]
    public void GcMonitor_100SnapshotsWithRecommendations_CompletesWithin3Seconds()
    {
        var logger = Substitute.For<ILogger<GcMonitor>>();
        var options = Options.Create(new ProfilingOptions { MaxRetainedSnapshots = 100 });
        var monitor = new GcMonitor(logger, options);

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100; i++)
        {
            monitor.CaptureSnapshot();
        }

        var recommendations = monitor.GetRecommendations();
        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)),
            $"100 GC snapshots + recommendations took {sw.ElapsedMilliseconds}ms");
        Assert.That(recommendations, Is.Not.Null);
    }
}
