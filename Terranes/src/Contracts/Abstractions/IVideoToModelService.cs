using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for AI video-to-3D conversion — upload a video of a house and generate a 3D model.
/// </summary>
public interface IVideoToModelService
{
    /// <summary>Uploads a video and starts the conversion job.</summary>
    Task<VideoToModelJob> UploadAsync(string fileName, long fileSizeBytes, int durationSeconds, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a conversion job by its unique identifier.</summary>
    Task<VideoToModelJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Gets all jobs for a user.</summary>
    Task<IReadOnlyList<VideoToModelJob>> GetJobsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Advances the job to the next processing stage (simulates async pipeline).</summary>
    Task<VideoToModelJob> AdvanceAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Marks a job as failed.</summary>
    Task<VideoToModelJob> FailJobAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default);
}
