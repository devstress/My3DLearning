using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class VideoToModelEndpoints
{
    public static void MapVideoToModelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/video-to-model").WithTags("Video-to-3D");

        group.MapPost("/upload", async (string fileName, long fileSizeBytes, int durationSeconds, Guid userId, IVideoToModelService service) =>
        {
            var job = await service.UploadAsync(fileName, fileSizeBytes, durationSeconds, userId);
            return Results.Created($"/api/video-to-model/jobs/{job.Id}", job);
        }).WithName("UploadVideo");

        group.MapGet("/jobs/{jobId:guid}", async (Guid jobId, IVideoToModelService service) =>
        {
            var job = await service.GetJobAsync(jobId);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        }).WithName("GetVideoJob");

        group.MapGet("/jobs/by-user/{userId:guid}", async (Guid userId, IVideoToModelService service) =>
        {
            var jobs = await service.GetJobsByUserAsync(userId);
            return Results.Ok(jobs);
        }).WithName("GetVideoJobsByUser");

        group.MapPost("/jobs/{jobId:guid}/advance", async (Guid jobId, IVideoToModelService service) =>
        {
            var job = await service.AdvanceAsync(jobId);
            return Results.Ok(job);
        }).WithName("AdvanceVideoJob");

        group.MapPost("/jobs/{jobId:guid}/fail", async (Guid jobId, string errorMessage, IVideoToModelService service) =>
        {
            var job = await service.FailJobAsync(jobId, errorMessage);
            return Results.Ok(job);
        }).WithName("FailVideoJob");
    }
}
