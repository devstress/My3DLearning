using Terranes.Contracts.Abstractions;

namespace Terranes.Platform.Api.Endpoints;

public static class EventBusEndpoints
{
    public static void MapEventBusEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events").WithTags("EventBus");

        group.MapPost("/", async (string topic, string payload, Guid correlationId, IEventBusService service) =>
        {
            var evt = await service.PublishAsync(topic, payload, correlationId);
            return Results.Created($"/api/events/{evt.Id}", evt);
        }).WithName("PublishEvent");

        group.MapGet("/topic/{topic}", async (string topic, IEventBusService service) =>
        {
            var events = await service.GetEventsForTopicAsync(topic);
            return Results.Ok(events);
        }).WithName("GetEventsByTopic");

        group.MapGet("/correlation/{correlationId:guid}", async (Guid correlationId, IEventBusService service) =>
        {
            var events = await service.GetEventsForCorrelationAsync(correlationId);
            return Results.Ok(events);
        }).WithName("GetEventsByCorrelation");

        group.MapGet("/count", async (IEventBusService service) =>
        {
            var count = await service.GetTotalEventCountAsync();
            return Results.Ok(new { TotalEvents = count });
        }).WithName("GetEventCount");

        group.MapGet("/topics", async (IEventBusService service) =>
        {
            var summary = await service.GetTopicSummaryAsync();
            return Results.Ok(summary);
        }).WithName("GetTopicSummary");
    }
}
