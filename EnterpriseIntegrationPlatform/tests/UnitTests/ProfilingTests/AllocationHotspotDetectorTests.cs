using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace UnitTests.ProfilingTests;

[TestFixture]
public class AllocationHotspotDetectorTests
{
    private ILogger<AllocationHotspotDetector> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<AllocationHotspotDetector>>();
    }

    private AllocationHotspotDetector BuildDetector(ProfilingOptions? options = null)
    {
        options ??= new ProfilingOptions();
        return new AllocationHotspotDetector(_logger, Options.Create(options));
    }

    [Test]
    public void RegisterOperation_ValidInput_TracksOperation()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(100), 1024);

        var stats = detector.GetOperationStats("TestOp");
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.InvocationCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterOperation_MultipleInvocations_AccumulatesStats()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(100), 1000);
        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(200), 2000);
        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(300), 3000);

        var stats = detector.GetOperationStats("TestOp");
        Assert.That(stats!.InvocationCount, Is.EqualTo(3));
        Assert.That(stats.AverageDuration.TotalMilliseconds, Is.EqualTo(200).Within(1));
        Assert.That(stats.AverageAllocatedBytes, Is.EqualTo(2000));
        Assert.That(stats.TotalAllocatedBytes, Is.EqualTo(6000));
    }

    [Test]
    public void RegisterOperation_TracksMinAndMaxDuration()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(100), 0);
        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(50), 0);
        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(300), 0);

        var stats = detector.GetOperationStats("TestOp");
        Assert.That(stats!.MinDuration.TotalMilliseconds, Is.EqualTo(50).Within(1));
        Assert.That(stats.MaxDuration.TotalMilliseconds, Is.EqualTo(300).Within(1));
    }

    [Test]
    public void RegisterOperation_NullName_ThrowsArgumentException()
    {
        var detector = BuildDetector();

        Assert.That(() => detector.RegisterOperation(null!, TimeSpan.FromSeconds(1), 0),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void RegisterOperation_EmptyName_ThrowsArgumentException()
    {
        var detector = BuildDetector();

        Assert.That(() => detector.RegisterOperation("", TimeSpan.FromSeconds(1), 0),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void RegisterOperation_NegativeAllocation_ThrowsArgumentOutOfRangeException()
    {
        var detector = BuildDetector();

        Assert.That(() => detector.RegisterOperation("TestOp", TimeSpan.FromSeconds(1), -1),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void RegisterOperation_ExceedsMaxTracked_IgnoresNewOperation()
    {
        var options = new ProfilingOptions { MaxTrackedOperations = 2 };
        var detector = BuildDetector(options);

        detector.RegisterOperation("Op1", TimeSpan.FromMilliseconds(1), 0);
        detector.RegisterOperation("Op2", TimeSpan.FromMilliseconds(1), 0);
        detector.RegisterOperation("Op3", TimeSpan.FromMilliseconds(1), 0);

        Assert.That(detector.GetOperationStats("Op3"), Is.Null);
    }

    [Test]
    public void RegisterOperation_ExceedsMaxTracked_StillUpdatesExisting()
    {
        var options = new ProfilingOptions { MaxTrackedOperations = 2 };
        var detector = BuildDetector(options);

        detector.RegisterOperation("Op1", TimeSpan.FromMilliseconds(1), 0);
        detector.RegisterOperation("Op2", TimeSpan.FromMilliseconds(1), 0);
        detector.RegisterOperation("Op1", TimeSpan.FromMilliseconds(2), 0);

        Assert.That(detector.GetOperationStats("Op1")!.InvocationCount, Is.EqualTo(2));
    }

    [Test]
    public void DetectHotspots_NoOperations_ReturnsEmpty()
    {
        var detector = BuildDetector();

        var hotspots = detector.DetectHotspots(new HotspotThresholds());

        Assert.That(hotspots, Is.Empty);
    }

    [Test]
    public void DetectHotspots_BelowMinimumInvocations_ReturnsEmpty()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds { MinimumInvocations = 5, DurationWarningMs = 100 };

        // Only 3 invocations — below minimum
        for (var i = 0; i < 3; i++)
            detector.RegisterOperation("SlowOp", TimeSpan.FromMilliseconds(500), 0);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Is.Empty);
    }

    [Test]
    public void DetectHotspots_DurationExceedsWarning_FlagsDurationHotspot()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 3,
            DurationWarningMs = 100,
            DurationCriticalMs = 500
        };

        for (var i = 0; i < 5; i++)
            detector.RegisterOperation("SlowOp", TimeSpan.FromMilliseconds(200), 0);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Has.Count.EqualTo(1));
        Assert.That(hotspots[0].Category, Is.EqualTo("Duration"));
        Assert.That(hotspots[0].Severity, Is.EqualTo(HotspotSeverity.Warning));
        Assert.That(hotspots[0].OperationName, Is.EqualTo("SlowOp"));
    }

    [Test]
    public void DetectHotspots_DurationExceedsCritical_FlagsCriticalHotspot()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 3,
            DurationWarningMs = 100,
            DurationCriticalMs = 500
        };

        for (var i = 0; i < 5; i++)
            detector.RegisterOperation("VerySlowOp", TimeSpan.FromMilliseconds(1000), 0);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Has.Count.EqualTo(1));
        Assert.That(hotspots[0].Severity, Is.EqualTo(HotspotSeverity.Critical));
    }

    [Test]
    public void DetectHotspots_AllocationExceedsWarning_FlagsAllocationHotspot()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 3,
            AllocationWarningBytes = 1000,
            AllocationCriticalBytes = 10000
        };

        for (var i = 0; i < 5; i++)
            detector.RegisterOperation("AllocOp", TimeSpan.FromMilliseconds(1), 5000);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Has.Count.EqualTo(1));
        Assert.That(hotspots[0].Category, Is.EqualTo("Allocation"));
        Assert.That(hotspots[0].Severity, Is.EqualTo(HotspotSeverity.Warning));
    }

    [Test]
    public void DetectHotspots_AllocationExceedsCritical_FlagsCriticalAllocationHotspot()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 3,
            AllocationWarningBytes = 1000,
            AllocationCriticalBytes = 10000
        };

        for (var i = 0; i < 5; i++)
            detector.RegisterOperation("BigAllocOp", TimeSpan.FromMilliseconds(1), 50000);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Has.Count.EqualTo(1));
        Assert.That(hotspots[0].Severity, Is.EqualTo(HotspotSeverity.Critical));
    }

    [Test]
    public void DetectHotspots_BothDurationAndAllocation_ReturnsTwoHotspots()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 3,
            DurationWarningMs = 100,
            AllocationWarningBytes = 1000
        };

        for (var i = 0; i < 5; i++)
            detector.RegisterOperation("BadOp", TimeSpan.FromMilliseconds(500), 5000);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots, Has.Count.EqualTo(2));
        Assert.That(hotspots.Select(h => h.Category), Is.EquivalentTo(new[] { "Duration", "Allocation" }));
    }

    [Test]
    public void DetectHotspots_NullThresholds_ThrowsArgumentNullException()
    {
        var detector = BuildDetector();

        Assert.That(() => detector.DetectHotspots(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void GetOperationStats_UnknownOperation_ReturnsNull()
    {
        var detector = BuildDetector();

        Assert.That(detector.GetOperationStats("Unknown"), Is.Null);
    }

    [Test]
    public void GetOperationStats_NullName_ThrowsArgumentException()
    {
        var detector = BuildDetector();

        Assert.That(() => detector.GetOperationStats(null!), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void GetAllOperationStats_ReturnsAllTracked()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("Op1", TimeSpan.FromMilliseconds(100), 0);
        detector.RegisterOperation("Op2", TimeSpan.FromMilliseconds(200), 0);

        var allStats = detector.GetAllOperationStats();

        Assert.That(allStats, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetAllOperationStats_OrderedByAverageDurationDescending()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("Fast", TimeSpan.FromMilliseconds(10), 0);
        detector.RegisterOperation("Slow", TimeSpan.FromMilliseconds(1000), 0);
        detector.RegisterOperation("Medium", TimeSpan.FromMilliseconds(500), 0);

        var allStats = detector.GetAllOperationStats();

        Assert.That(allStats[0].OperationName, Is.EqualTo("Slow"));
        Assert.That(allStats[1].OperationName, Is.EqualTo("Medium"));
        Assert.That(allStats[2].OperationName, Is.EqualTo("Fast"));
    }

    [Test]
    public void Reset_ClearsAllOperations()
    {
        var detector = BuildDetector();

        detector.RegisterOperation("Op1", TimeSpan.FromMilliseconds(100), 0);
        detector.Reset();

        Assert.That(detector.GetAllOperationStats(), Is.Empty);
        Assert.That(detector.GetOperationStats("Op1"), Is.Null);
    }

    [Test]
    public void DetectHotspots_MeasuredAndThresholdValues_CorrectlySet()
    {
        var detector = BuildDetector();
        var thresholds = new HotspotThresholds
        {
            MinimumInvocations = 1,
            DurationWarningMs = 50
        };

        detector.RegisterOperation("TestOp", TimeSpan.FromMilliseconds(100), 0);

        var hotspots = detector.DetectHotspots(thresholds);

        Assert.That(hotspots[0].MeasuredValue, Is.EqualTo(100).Within(1));
        Assert.That(hotspots[0].ThresholdValue, Is.EqualTo(50));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(() => new AllocationHotspotDetector(null!, Options.Create(new ProfilingOptions())),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.That(() => new AllocationHotspotDetector(_logger, null!),
            Throws.ArgumentNullException);
    }
}
