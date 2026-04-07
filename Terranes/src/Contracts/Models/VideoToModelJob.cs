using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a video upload for AI video-to-3D conversion.
/// </summary>
/// <param name="Id">Unique identifier for the video job.</param>
/// <param name="FileName">Original file name of the uploaded video.</param>
/// <param name="FileSizeBytes">Size of the video file in bytes.</param>
/// <param name="DurationSeconds">Duration of the video in seconds.</param>
/// <param name="Status">Current processing status.</param>
/// <param name="GeneratedHomeModelId">Reference to the generated <see cref="HomeModel"/>, or null if not yet complete.</param>
/// <param name="ErrorMessage">Error message if processing failed.</param>
/// <param name="UploadedByUserId">User who uploaded the video.</param>
/// <param name="UploadedAtUtc">UTC timestamp when the video was uploaded.</param>
/// <param name="CompletedAtUtc">UTC timestamp when processing completed, or null.</param>
public sealed record VideoToModelJob(
    Guid Id,
    string FileName,
    long FileSizeBytes,
    int DurationSeconds,
    VideoProcessingStatus Status,
    Guid? GeneratedHomeModelId,
    string? ErrorMessage,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAtUtc,
    DateTimeOffset? CompletedAtUtc);
