using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;

namespace Terranes.Platform.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapPost("/track", async (Guid userId, Guid tenantId, AnalyticsEventType eventType, Guid? entityId, string? metadata, IAnalyticsService service) =>
        {
            var evt = await service.TrackAsync(userId, tenantId, eventType, entityId, metadata);
            return Results.Created($"/api/analytics/{evt.Id}", evt);
        }).WithName("TrackEvent");

        group.MapGet("/user/{userId:guid}", async (Guid userId, IAnalyticsService service) =>
        {
            var events = await service.GetUserEventsAsync(userId);
            return Results.Ok(events);
        }).WithName("GetUserAnalytics");

        group.MapGet("/summary/{eventType}", async (AnalyticsEventType eventType, DateTimeOffset from, DateTimeOffset to, IAnalyticsService service) =>
        {
            var summary = await service.GetSummaryAsync(eventType, from, to);
            return Results.Ok(summary);
        }).WithName("GetAnalyticsSummary");

        group.MapGet("/popular/{eventType}", async (AnalyticsEventType eventType, int? top, IAnalyticsService service) =>
        {
            var popular = await service.GetPopularEntitiesAsync(eventType, top ?? 10);
            return Results.Ok(popular);
        }).WithName("GetPopularEntities");

        group.MapGet("/count", async (IAnalyticsService service) =>
        {
            var count = await service.GetTotalEventCountAsync();
            return Results.Ok(new { TotalEvents = count });
        }).WithName("GetAnalyticsEventCount");
    }
}
