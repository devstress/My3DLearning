// ============================================================================
// Tutorial 45 – Performance Profiling (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — multiple snapshots_ time range query_ publish analysis
//   🟡 Intermediate  — snapshot delta metrics_ cpu usage tracking
//   🔴 Advanced      — profiling session lifecycle_ publish all snapshots
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Performance.Profiling;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial45;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_MultipleSnapshots_TimeRangeQuery_PublishAnalysis()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-range");
        var topic = AspireFixture.UniqueTopic("t45-exam-analysis-results");

        // TODO: Create a ContinuousProfiler with appropriate configuration
        dynamic profiler = null!;

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var s1 = profiler.CaptureSnapshot("baseline");
        var s2 = profiler.CaptureSnapshot("under-load");
        var s3 = profiler.CaptureSnapshot("post-load");

        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var snapshots = profiler.GetSnapshots(before, after);
        Assert.That(snapshots, Has.Count.EqualTo(3));
        Assert.That(snapshots[0].Label, Is.EqualTo("baseline"));
        Assert.That(snapshots[1].Label, Is.EqualTo("under-load"));
        Assert.That(snapshots[2].Label, Is.EqualTo("post-load"));

        // Verify ordering
        Assert.That(snapshots[0].CapturedAt, Is.LessThanOrEqualTo(snapshots[1].CapturedAt));
        Assert.That(snapshots[1].CapturedAt, Is.LessThanOrEqualTo(snapshots[2].CapturedAt));

        foreach (var snap in snapshots)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Intermediate_SnapshotDeltaMetrics_CpuUsageTracking()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-delta");
        var topic = AspireFixture.UniqueTopic("t45-exam-delta-results");

        // TODO: Create a ContinuousProfiler with appropriate configuration
        dynamic profiler = null!;

        // First snapshot has no CPU delta (no baseline)
        var first = profiler.CaptureSnapshot("first");
        Assert.That(first.Cpu.CpuUsagePercent, Is.Null);

        // Second snapshot should have a CPU delta computed
        var second = profiler.CaptureSnapshot("second");
        Assert.That(second.Cpu.TotalProcessorTime, Is.GreaterThanOrEqualTo(first.Cpu.TotalProcessorTime));
        Assert.That(second.Memory.TotalAllocatedBytes, Is.GreaterThanOrEqualTo(first.Memory.TotalAllocatedBytes));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await nats.PublishAsync(...)
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Advanced_ProfilingSessionLifecycle_PublishAllSnapshots()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-session");
        var topic = AspireFixture.UniqueTopic("t45-exam-session-stream");

        // TODO: Create a ContinuousProfiler with appropriate configuration
        dynamic profiler = null!;

        // Simulate a profiling session: capture, query, verify
        Assert.That(profiler.SnapshotCount, Is.EqualTo(0));
        Assert.That(profiler.GetLatestSnapshot(), Is.Null);

        var labels = new[] { "startup", "warmup", "steady-state", "peak", "cooldown" };
        foreach (var label in labels)
        {
            profiler.CaptureSnapshot(label);
        }

        Assert.That(profiler.SnapshotCount, Is.EqualTo(5));
        Assert.That(profiler.GetLatestSnapshot()!.Label, Is.EqualTo("cooldown"));

        var allSnapshots = profiler.GetSnapshots(
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.That(allSnapshots, Has.Count.EqualTo(5));

        foreach (var snap in allSnapshots)
        {
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: await nats.PublishAsync(...)
        }

        nats.AssertReceivedOnTopic(topic, 5);

        var all = nats.GetAllReceived<string>(topic);
        Assert.That(all[0].Payload, Does.StartWith("startup|"));
        Assert.That(all[4].Payload, Does.StartWith("cooldown|"));
    }
}
#endif
