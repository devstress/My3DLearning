using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Immersive3D;

/// <summary>
/// In-memory implementation of <see cref="IVideoToModelService"/>.
/// Simulates AI video-to-3D conversion pipeline with stage progression.
/// </summary>
public sealed class VideoToModelService : IVideoToModelService
{
    private static readonly string[] SupportedExtensions = [".mp4", ".mov", ".avi", ".webm"];
    private const long MaxVideoSizeBytes = 2L * 1024 * 1024 * 1024; // 2 GB
    private const int MaxDurationSeconds = 600; // 10 minutes

    private readonly ConcurrentDictionary<Guid, VideoToModelJob> _jobs = new();
    private readonly ILogger<VideoToModelService> _logger;

    public VideoToModelService(ILogger<VideoToModelService> logger) => _logger = logger;

    public Task<VideoToModelJob> UploadAsync(string fileName, long fileSizeBytes, int durationSeconds, Guid userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            throw new ArgumentException($"Unsupported video format: {extension}. Supported: {string.Join(", ", SupportedExtensions)}", nameof(fileName));

        if (fileSizeBytes <= 0 || fileSizeBytes > MaxVideoSizeBytes)
            throw new ArgumentException($"File size must be between 1 byte and {MaxVideoSizeBytes} bytes.", nameof(fileSizeBytes));

        if (durationSeconds <= 0 || durationSeconds > MaxDurationSeconds)
            throw new ArgumentException($"Duration must be between 1 and {MaxDurationSeconds} seconds.", nameof(durationSeconds));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        var job = new VideoToModelJob(
            Guid.NewGuid(), fileName, fileSizeBytes, durationSeconds,
            VideoProcessingStatus.Queued, null, null, userId,
            DateTimeOffset.UtcNow, null);

        if (!_jobs.TryAdd(job.Id, job))
            throw new InvalidOperationException($"Job {job.Id} already exists.");

        _logger.LogInformation("Uploaded video {JobId}", job.Id);
        return Task.FromResult(job);
    }

    public Task<VideoToModelJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<VideoToModelJob>> GetJobsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VideoToModelJob> result = _jobs.Values
            .Where(j => j.UploadedByUserId == userId)
            .OrderByDescending(j => j.UploadedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<VideoToModelJob> AdvanceAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            throw new InvalidOperationException($"Job {jobId} not found.");

        var nextStatus = job.Status switch
        {
            VideoProcessingStatus.Queued => VideoProcessingStatus.Analysing,
            VideoProcessingStatus.Analysing => VideoProcessingStatus.GeneratingMesh,
            VideoProcessingStatus.GeneratingMesh => VideoProcessingStatus.Completed,
            _ => throw new InvalidOperationException($"Cannot advance job in status {job.Status}.")
        };

        var updated = job with
        {
            Status = nextStatus,
            GeneratedHomeModelId = nextStatus == VideoProcessingStatus.Completed ? Guid.NewGuid() : job.GeneratedHomeModelId,
            CompletedAtUtc = nextStatus == VideoProcessingStatus.Completed ? DateTimeOffset.UtcNow : null
        };

        _jobs[jobId] = updated;

        _logger.LogInformation("Advanced job {JobId} to {Status}", jobId, nextStatus);
        return Task.FromResult(updated);
    }

    public Task<VideoToModelJob> FailJobAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            throw new InvalidOperationException($"Job {jobId} not found.");

        if (job.Status == VideoProcessingStatus.Completed || job.Status == VideoProcessingStatus.Failed)
            throw new InvalidOperationException($"Cannot fail job in status {job.Status}.");

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required.", nameof(errorMessage));

        var updated = job with { Status = VideoProcessingStatus.Failed, ErrorMessage = errorMessage, CompletedAtUtc = DateTimeOffset.UtcNow };
        _jobs[jobId] = updated;

        _logger.LogInformation("Job {JobId} failed", jobId);
        return Task.FromResult(updated);
    }
}
