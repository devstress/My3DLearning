// ============================================================================
// Tutorial 45 – Performance Profiling (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — multiple snapshots_ time range query_ publish analysis
//   🟡 Intermediate — snapshot delta metrics_ cpu usage tracking
//   🔴 Advanced     — profiling session lifecycle_ publish all snapshots
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Performance.Profiling;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial45;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_MultipleSnapshots_TimeRangeQuery_PublishAnalysis()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-range");
        var topic = AspireFixture.UniqueTopic("t45-exam-analysis-results");

        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

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
            var envelope = IntegrationEnvelope<string>.Create(
                $"{snap.Label}|threads:{snap.Cpu.ThreadCount}|ws:{snap.Memory.WorkingSetBytes}",
                "profiler", "snapshot.analysis");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task Intermediate_SnapshotDeltaMetrics_CpuUsageTracking()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-delta");
        var topic = AspireFixture.UniqueTopic("t45-exam-delta-results");

        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions()));

        // First snapshot has no CPU delta (no baseline)
        var first = profiler.CaptureSnapshot("first");
        Assert.That(first.Cpu.CpuUsagePercent, Is.Null);

        // Second snapshot should have a CPU delta computed
        var second = profiler.CaptureSnapshot("second");
        Assert.That(second.Cpu.TotalProcessorTime, Is.GreaterThanOrEqualTo(first.Cpu.TotalProcessorTime));
        Assert.That(second.Memory.TotalAllocatedBytes, Is.GreaterThanOrEqualTo(first.Memory.TotalAllocatedBytes));

        var envelope = IntegrationEnvelope<string>.Create(
            $"cpu-delta-available:{second.Cpu.CpuUsagePercent is not null}|" +
            $"alloc-delta-available:{second.Memory.AllocationRateBytesPerSecond is not null}",
            "profiler", "delta.metrics");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task Advanced_ProfilingSessionLifecycle_PublishAllSnapshots()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-exam-session");
        var topic = AspireFixture.UniqueTopic("t45-exam-session-stream");

        var profiler = new ContinuousProfiler(
            NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions { MaxRetainedSnapshots = 5 }));

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
            var envelope = IntegrationEnvelope<string>.Create(
                $"{snap.Label}|gc-gen2:{snap.Gc.Gen2Collections}",
                "profiler", "session.snapshot");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 5);

        var all = nats.GetAllReceived<string>(topic);
        Assert.That(all[0].Payload, Does.StartWith("startup|"));
        Assert.That(all[4].Payload, Does.StartWith("cooldown|"));
    }
}
