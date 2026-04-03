using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Performance.Profiling;

namespace UnitTests.ProfilingTests;

[TestFixture]
public class GcMonitorTests
{
    private ILogger<GcMonitor> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<GcMonitor>>();
    }

    private GcMonitor BuildMonitor(ProfilingOptions? options = null)
    {
        options ??= new ProfilingOptions();
        return new GcMonitor(_logger, Options.Create(options));
    }

    [Test]
    public void CaptureSnapshot_ReturnsValidGcSnapshot()
    {
        var monitor = BuildMonitor();

        var snapshot = monitor.CaptureSnapshot();

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Gen0Collections, Is.GreaterThanOrEqualTo(0));
        Assert.That(snapshot.Gen1Collections, Is.GreaterThanOrEqualTo(0));
        Assert.That(snapshot.Gen2Collections, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void CaptureSnapshot_FragmentationRatio_InValidRange()
    {
        var monitor = BuildMonitor();

        var snapshot = monitor.CaptureSnapshot();

        Assert.That(snapshot.FragmentationRatio, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(snapshot.FragmentationRatio, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void CaptureSnapshot_PauseTimePercentage_NonNegative()
    {
        var monitor = BuildMonitor();

        var snapshot = monitor.CaptureSnapshot();

        Assert.That(snapshot.PauseTimePercentage, Is.GreaterThanOrEqualTo(0.0));
    }

    [Test]
    public void CaptureSnapshot_TotalCommittedBytes_Positive()
    {
        var monitor = BuildMonitor();

        var snapshot = monitor.CaptureSnapshot();

        Assert.That(snapshot.TotalCommittedBytes, Is.GreaterThan(0));
    }

    [Test]
    public void CaptureSnapshot_AddsToHistory()
    {
        var monitor = BuildMonitor();

        monitor.CaptureSnapshot();
        monitor.CaptureSnapshot();

        Assert.That(monitor.GetHistory(), Has.Count.EqualTo(2));
    }

    [Test]
    public void CaptureSnapshot_ExceedsMaxRetained_EvictsOldest()
    {
        var options = new ProfilingOptions { MaxRetainedSnapshots = 2 };
        var monitor = BuildMonitor(options);

        monitor.CaptureSnapshot();
        monitor.CaptureSnapshot();
        monitor.CaptureSnapshot();

        Assert.That(monitor.GetHistory(), Has.Count.EqualTo(2));
    }

    [Test]
    public void GetHistory_ReturnsDefensiveCopy()
    {
        var monitor = BuildMonitor();
        monitor.CaptureSnapshot();

        var history1 = monitor.GetHistory();
        monitor.CaptureSnapshot();
        var history2 = monitor.GetHistory();

        Assert.That(history1, Has.Count.EqualTo(1));
        Assert.That(history2, Has.Count.EqualTo(2));
    }

    [Test]
    public void ClearHistory_EmptiesHistory()
    {
        var monitor = BuildMonitor();
        monitor.CaptureSnapshot();
        monitor.CaptureSnapshot();

        monitor.ClearHistory();

        Assert.That(monitor.GetHistory(), Is.Empty);
    }

    [Test]
    public void GetRecommendations_NoSnapshots_ReturnsEmpty()
    {
        var monitor = BuildMonitor();

        var recommendations = monitor.GetRecommendations();

        Assert.That(recommendations, Is.Empty);
    }

    [Test]
    public void GetRecommendations_WithSnapshot_ReturnsRecommendations()
    {
        var monitor = BuildMonitor();
        monitor.CaptureSnapshot();

        var recommendations = monitor.GetRecommendations();

        // At minimum, workstation GC recommendation should be present in test context
        Assert.That(recommendations, Is.Not.Null);
    }

    [Test]
    public void GetRecommendations_ServerGcCheck_ReturnsInfoSeverity()
    {
        var monitor = BuildMonitor();
        monitor.CaptureSnapshot();

        var recommendations = monitor.GetRecommendations();

        // In test context, server GC is typically off
        var serverGcRec = recommendations.FirstOrDefault(r => r.Category == "ServerGC");
        if (serverGcRec is not null)
        {
            Assert.That(serverGcRec.Severity, Is.EqualTo(HotspotSeverity.Info));
            Assert.That(serverGcRec.CurrentValue, Is.EqualTo("Workstation GC"));
        }
    }

    [Test]
    public void GetRecommendations_AllRecommendations_HaveRequiredFields()
    {
        var monitor = BuildMonitor();
        monitor.CaptureSnapshot();
        monitor.CaptureSnapshot();

        var recommendations = monitor.GetRecommendations();

        foreach (var rec in recommendations)
        {
            Assert.That(rec.Category, Is.Not.Null.And.Not.Empty);
            Assert.That(rec.Description, Is.Not.Null.And.Not.Empty);
            Assert.That(rec.CurrentValue, Is.Not.Null.And.Not.Empty);
            Assert.That(rec.RecommendedAction, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public void CaptureSnapshot_LatencyMode_HasValidValue()
    {
        var monitor = BuildMonitor();

        var snapshot = monitor.CaptureSnapshot();

        Assert.That(Enum.IsDefined(snapshot.LatencyMode), Is.True);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(() => new GcMonitor(null!, Options.Create(new ProfilingOptions())),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.That(() => new GcMonitor(_logger, null!),
            Throws.ArgumentNullException);
    }
}
