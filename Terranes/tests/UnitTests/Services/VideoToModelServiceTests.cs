using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Immersive3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class VideoToModelServiceTests
{
    private VideoToModelService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new VideoToModelService(NullLogger<VideoToModelService>.Instance);

    // ── 1. Upload Validation ──

    [Test]
    public async Task UploadAsync_ValidVideo_ReturnsQueuedJob()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000_000, 120, Guid.NewGuid());

        Assert.That(job.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(job.Status, Is.EqualTo(VideoProcessingStatus.Queued));
        Assert.That(job.FileName, Is.EqualTo("house.mp4"));
    }

    [Test]
    public void UploadAsync_UnsupportedFormat_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync("house.mkv", 100_000, 60, Guid.NewGuid()));
    }

    [Test]
    public void UploadAsync_ExceedsMaxSize_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync("house.mp4", 3L * 1024 * 1024 * 1024, 60, Guid.NewGuid()));
    }

    [Test]
    public void UploadAsync_ExceedsDuration_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync("house.mp4", 100_000, 700, Guid.NewGuid()));
    }

    [Test]
    public void UploadAsync_EmptyFileName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync("", 100_000, 60, Guid.NewGuid()));
    }

    [Test]
    public void UploadAsync_EmptyUserId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync("house.mp4", 100_000, 60, Guid.Empty));
    }

    // ── 2. Pipeline Progression ──

    [Test]
    public async Task AdvanceAsync_QueuedToAnalysing_UpdatesStatus()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000, 120, Guid.NewGuid());
        var advanced = await _sut.AdvanceAsync(job.Id);

        Assert.That(advanced.Status, Is.EqualTo(VideoProcessingStatus.Analysing));
    }

    [Test]
    public async Task AdvanceAsync_FullPipeline_CompletesWithModel()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000, 120, Guid.NewGuid());
        await _sut.AdvanceAsync(job.Id); // → Analysing
        await _sut.AdvanceAsync(job.Id); // → GeneratingMesh
        var completed = await _sut.AdvanceAsync(job.Id); // → Completed

        Assert.That(completed.Status, Is.EqualTo(VideoProcessingStatus.Completed));
        Assert.That(completed.GeneratedHomeModelId, Is.Not.Null);
        Assert.That(completed.CompletedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task AdvanceAsync_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000, 120, Guid.NewGuid());
        await _sut.AdvanceAsync(job.Id);
        await _sut.AdvanceAsync(job.Id);
        await _sut.AdvanceAsync(job.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AdvanceAsync(job.Id));
    }

    // ── 3. Failure & Retrieval ──

    [Test]
    public async Task FailJobAsync_ValidJob_SetsFailedStatus()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000, 120, Guid.NewGuid());
        var failed = await _sut.FailJobAsync(job.Id, "Insufficient frames for reconstruction");

        Assert.That(failed.Status, Is.EqualTo(VideoProcessingStatus.Failed));
        Assert.That(failed.ErrorMessage, Is.EqualTo("Insufficient frames for reconstruction"));
    }

    [Test]
    public async Task FailJobAsync_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        var job = await _sut.UploadAsync("house.mp4", 100_000, 120, Guid.NewGuid());
        await _sut.AdvanceAsync(job.Id);
        await _sut.AdvanceAsync(job.Id);
        await _sut.AdvanceAsync(job.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.FailJobAsync(job.Id, "Too late"));
    }

    [Test]
    public async Task GetJobsByUserAsync_ReturnsUserJobs()
    {
        var userId = Guid.NewGuid();
        await _sut.UploadAsync("house1.mp4", 100_000, 60, userId);
        await _sut.UploadAsync("house2.mov", 200_000, 90, userId);
        await _sut.UploadAsync("other.mp4", 100_000, 60, Guid.NewGuid());

        var jobs = await _sut.GetJobsByUserAsync(userId);
        Assert.That(jobs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetJobAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetJobAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }
}
