// ============================================================================
// Tutorial 45 – Performance Profiling (Lab)
// ============================================================================
// EIP Pattern: Profiling.
// E2E: ContinuousProfiler — capture snapshots, query by time range,
//      publish profiling results to NatsBrokerEndpoint (real NATS JetStream
//      via Aspire).
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Performance.Profiling;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial45;

[TestFixture]
public sealed class Lab
{
    private static ContinuousProfiler CreateProfiler(int maxSnapshots = 1000) =>
        new(NullLogger<ContinuousProfiler>.Instance,
            Options.Create(new ProfilingOptions { MaxRetainedSnapshots = maxSnapshots }));


    // ── 1. Snapshot Capture ──────────────────────────────────────────

    [Test]
    public async Task CaptureSnapshot_PublishMetricsToNatsBrokerEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-capture");
        var topic = AspireFixture.UniqueTopic("t45-profiling-metrics");

        var profiler = CreateProfiler();

        var snapshot = profiler.CaptureSnapshot("baseline");
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Label, Is.EqualTo("baseline"));
        Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
        Assert.That(snapshot.Cpu, Is.Not.Null);
        Assert.That(snapshot.Memory, Is.Not.Null);
        Assert.That(snapshot.Gc, Is.Not.Null);

        var envelope = IntegrationEnvelope<string>.Create(
            $"cpu-threads:{snapshot.Cpu.ThreadCount}", "profiler", "snapshot.captured");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task SnapshotCount_Increments_PublishCount()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-count");
        var topic = AspireFixture.UniqueTopic("t45-profiling-stats");

        var profiler = CreateProfiler();

        Assert.That(profiler.SnapshotCount, Is.EqualTo(0));

        profiler.CaptureSnapshot();
        profiler.CaptureSnapshot();
        profiler.CaptureSnapshot();
        Assert.That(profiler.SnapshotCount, Is.EqualTo(3));

        var envelope = IntegrationEnvelope<string>.Create(
            $"count:{profiler.SnapshotCount}", "profiler", "snapshot.count");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }


    // ── 2. Time-Range Queries ────────────────────────────────────────

    [Test]
    public async Task GetLatestSnapshot_PublishLabel()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-latest");
        var topic = AspireFixture.UniqueTopic("t45-latest-snapshot");

        var profiler = CreateProfiler();

        Assert.That(profiler.GetLatestSnapshot(), Is.Null);

        profiler.CaptureSnapshot("first");
        profiler.CaptureSnapshot("second");
        var last = profiler.CaptureSnapshot("third");

        var latest = profiler.GetLatestSnapshot();
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.SnapshotId, Is.EqualTo(last.SnapshotId));
        Assert.That(latest.Label, Is.EqualTo("third"));

        var envelope = IntegrationEnvelope<string>.Create(
            latest.Label!, "profiler", "snapshot.latest");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task GetSnapshotsByTimeRange_PublishFiltered()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-range");
        var topic = AspireFixture.UniqueTopic("t45-range-results");

        var profiler = CreateProfiler();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        profiler.CaptureSnapshot("snap-1");
        profiler.CaptureSnapshot("snap-2");
        profiler.CaptureSnapshot("snap-3");

        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var snapshots = profiler.GetSnapshots(before, after);
        Assert.That(snapshots, Has.Count.EqualTo(3));
        Assert.That(snapshots[0].CapturedAt, Is.LessThanOrEqualTo(snapshots[1].CapturedAt));

        foreach (var snap in snapshots)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                snap.Label!, "profiler", "snapshot.range");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);

        // Narrow range should return empty
        var empty = profiler.GetSnapshots(
            DateTimeOffset.UtcNow.AddMinutes(1), DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.That(empty, Is.Empty);
    }


    // ── 3. Retention & Eviction ──────────────────────────────────────

    [Test]
    public async Task LabelledSnapshots_PublishWithMetadata()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-labelled");
        var topic = AspireFixture.UniqueTopic("t45-labelled-snapshots");

        var profiler = CreateProfiler();

        var s1 = profiler.CaptureSnapshot("before-load");
        var s2 = profiler.CaptureSnapshot("during-load");
        var s3 = profiler.CaptureSnapshot("after-load");

        Assert.That(s1.Label, Is.EqualTo("before-load"));
        Assert.That(s2.Label, Is.EqualTo("during-load"));
        Assert.That(s3.Label, Is.EqualTo("after-load"));
        Assert.That(s3.Memory.WorkingSetBytes, Is.GreaterThan(0));

        foreach (var snap in new[] { s1, s2, s3 })
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{snap.Label}|ws:{snap.Memory.WorkingSetBytes}", "profiler", "snapshot.labelled");
            await nats.PublishAsync(envelope, topic, default);
        }

        nats.AssertReceivedOnTopic(topic, 3);
    }

    [Test]
    public async Task MaxRetention_EvictsOldest_PublishCurrent()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t45-retention");
        var topic = AspireFixture.UniqueTopic("t45-retention-results");

        var profiler = CreateProfiler(maxSnapshots: 3);

        profiler.CaptureSnapshot("s1");
        profiler.CaptureSnapshot("s2");
        profiler.CaptureSnapshot("s3");
        profiler.CaptureSnapshot("s4");

        Assert.That(profiler.SnapshotCount, Is.EqualTo(3));

        var latest = profiler.GetLatestSnapshot();
        Assert.That(latest!.Label, Is.EqualTo("s4"));

        var envelope = IntegrationEnvelope<string>.Create(
            $"retained:{profiler.SnapshotCount}", "profiler", "snapshot.retention");
        await nats.PublishAsync(envelope, topic, default);
        nats.AssertReceivedOnTopic(topic, 1);
    }
}
