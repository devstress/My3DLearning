using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace UnitTests.ProfilingTests;

[TestFixture]
public class InMemoryBenchmarkRegistryTests
{
    private ILogger<InMemoryBenchmarkRegistry> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<InMemoryBenchmarkRegistry>>();
    }

    private InMemoryBenchmarkRegistry BuildRegistry()
    {
        return new InMemoryBenchmarkRegistry(_logger);
    }

    private static BenchmarkBaseline BuildBaseline(
        string name = "TestBenchmark",
        double meanMs = 100,
        long meanBytes = 1024,
        int iterations = 100,
        double regressionThreshold = 20.0)
    {
        return new BenchmarkBaseline
        {
            BenchmarkName = name,
            MeanDuration = TimeSpan.FromMilliseconds(meanMs),
            MeanAllocatedBytes = meanBytes,
            Iterations = iterations,
            RecordedAt = DateTimeOffset.UtcNow,
            RegressionThresholdPercent = regressionThreshold
        };
    }

    private static BenchmarkResult BuildResult(
        string name = "TestBenchmark",
        double meanMs = 100,
        long meanBytes = 1024,
        int iterations = 100)
    {
        return new BenchmarkResult
        {
            BenchmarkName = name,
            MeanDuration = TimeSpan.FromMilliseconds(meanMs),
            MeanAllocatedBytes = meanBytes,
            Iterations = iterations,
            RunAt = DateTimeOffset.UtcNow
        };
    }

    [Test]
    public void RegisterBaseline_ValidBaseline_Stores()
    {
        var registry = BuildRegistry();
        var baseline = BuildBaseline();

        registry.RegisterBaseline(baseline);

        Assert.That(registry.GetBaseline("TestBenchmark"), Is.EqualTo(baseline));
    }

    [Test]
    public void RegisterBaseline_SameName_ReplacesExisting()
    {
        var registry = BuildRegistry();
        var first = BuildBaseline(meanMs: 100);
        var second = BuildBaseline(meanMs: 200);

        registry.RegisterBaseline(first);
        registry.RegisterBaseline(second);

        var stored = registry.GetBaseline("TestBenchmark");
        Assert.That(stored!.MeanDuration.TotalMilliseconds, Is.EqualTo(200));
    }

    [Test]
    public void RegisterBaseline_NullBaseline_ThrowsArgumentNullException()
    {
        var registry = BuildRegistry();

        Assert.That(() => registry.RegisterBaseline(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void GetBaseline_Unknown_ReturnsNull()
    {
        var registry = BuildRegistry();

        Assert.That(registry.GetBaseline("Unknown"), Is.Null);
    }

    [Test]
    public void GetBaseline_NullName_ThrowsArgumentException()
    {
        var registry = BuildRegistry();

        Assert.That(() => registry.GetBaseline(null!), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void GetBaseline_CaseInsensitive()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline("MyBenchmark"));

        Assert.That(registry.GetBaseline("mybenchmark"), Is.Not.Null);
        Assert.That(registry.GetBaseline("MYBENCHMARK"), Is.Not.Null);
    }

    [Test]
    public void Compare_NoBaseline_ReturnsNull()
    {
        var registry = BuildRegistry();

        var result = registry.Compare(BuildResult("NoBaseline"));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Compare_WithinThreshold_NoRegression()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanMs: 100, meanBytes: 1000, regressionThreshold: 20));

        var regression = registry.Compare(BuildResult(meanMs: 110, meanBytes: 1100));

        Assert.That(regression, Is.Not.Null);
        Assert.That(regression!.HasRegression, Is.False);
        Assert.That(regression.DurationRegressed, Is.False);
        Assert.That(regression.AllocationRegressed, Is.False);
    }

    [Test]
    public void Compare_DurationExceedsThreshold_DurationRegressed()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanMs: 100, regressionThreshold: 20));

        var regression = registry.Compare(BuildResult(meanMs: 130));

        Assert.That(regression!.DurationRegressed, Is.True);
        Assert.That(regression.DurationChangePercent, Is.EqualTo(30).Within(0.1));
    }

    [Test]
    public void Compare_AllocationExceedsThreshold_AllocationRegressed()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanBytes: 1000, regressionThreshold: 20));

        var regression = registry.Compare(BuildResult(meanBytes: 1300));

        Assert.That(regression!.AllocationRegressed, Is.True);
        Assert.That(regression.AllocationChangePercent, Is.EqualTo(30).Within(0.1));
    }

    [Test]
    public void Compare_BothExceedThreshold_BothRegressed()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanMs: 100, meanBytes: 1000, regressionThreshold: 10));

        var regression = registry.Compare(BuildResult(meanMs: 150, meanBytes: 1500));

        Assert.That(regression!.DurationRegressed, Is.True);
        Assert.That(regression.AllocationRegressed, Is.True);
        Assert.That(regression.HasRegression, Is.True);
    }

    [Test]
    public void Compare_Improvement_NegativeChangePercent()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanMs: 100, meanBytes: 1000));

        var regression = registry.Compare(BuildResult(meanMs: 80, meanBytes: 800));

        Assert.That(regression!.DurationChangePercent, Is.LessThan(0));
        Assert.That(regression.AllocationChangePercent, Is.LessThan(0));
        Assert.That(regression.HasRegression, Is.False);
    }

    [Test]
    public void Compare_IncludesBaselineAndCurrentInResult()
    {
        var registry = BuildRegistry();
        var baseline = BuildBaseline();
        registry.RegisterBaseline(baseline);

        var result = BuildResult();
        var regression = registry.Compare(result);

        Assert.That(regression!.Baseline, Is.EqualTo(baseline));
        Assert.That(regression.Current, Is.EqualTo(result));
    }

    [Test]
    public void Compare_NullResult_ThrowsArgumentNullException()
    {
        var registry = BuildRegistry();

        Assert.That(() => registry.Compare(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void GetAllBaselines_ReturnsAll()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline("B1"));
        registry.RegisterBaseline(BuildBaseline("B2"));
        registry.RegisterBaseline(BuildBaseline("B3"));

        var all = registry.GetAllBaselines();

        Assert.That(all, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetAllBaselines_OrderedByName()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline("Charlie"));
        registry.RegisterBaseline(BuildBaseline("Alpha"));
        registry.RegisterBaseline(BuildBaseline("Bravo"));

        var all = registry.GetAllBaselines();

        Assert.That(all[0].BenchmarkName, Is.EqualTo("Alpha"));
        Assert.That(all[1].BenchmarkName, Is.EqualTo("Bravo"));
        Assert.That(all[2].BenchmarkName, Is.EqualTo("Charlie"));
    }

    [Test]
    public void RemoveBaseline_ExistingBaseline_ReturnsTrue()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline("ToRemove"));

        Assert.That(registry.RemoveBaseline("ToRemove"), Is.True);
        Assert.That(registry.GetBaseline("ToRemove"), Is.Null);
    }

    [Test]
    public void RemoveBaseline_Unknown_ReturnsFalse()
    {
        var registry = BuildRegistry();

        Assert.That(registry.RemoveBaseline("Unknown"), Is.False);
    }

    [Test]
    public void RemoveBaseline_NullName_ThrowsArgumentException()
    {
        var registry = BuildRegistry();

        Assert.That(() => registry.RemoveBaseline(null!), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Compare_ZeroBaseline_Duration_NoRegression()
    {
        var registry = BuildRegistry();
        registry.RegisterBaseline(BuildBaseline(meanMs: 0, meanBytes: 0));

        var regression = registry.Compare(BuildResult(meanMs: 50, meanBytes: 50));

        Assert.That(regression, Is.Not.Null);
        Assert.That(regression!.DurationChangePercent, Is.EqualTo(0));
        Assert.That(regression.AllocationChangePercent, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(() => new InMemoryBenchmarkRegistry(null!), Throws.ArgumentNullException);
    }
}
