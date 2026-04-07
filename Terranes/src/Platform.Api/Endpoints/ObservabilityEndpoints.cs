using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class ObservabilityEndpoints
{
    public static void MapObservabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/observability").WithTags("Observability");

        group.MapPost("/audit", async (AuditLogEntry entry, IObservabilityService service) =>
        {
            var created = await service.LogAuditAsync(entry);
            return Results.Created($"/api/observability/audit/{created.Id}", created);
        }).WithName("LogAudit");

        group.MapGet("/audit/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, IObservabilityService service) =>
        {
            var entries = await service.GetAuditLogAsync(entityType, entityId);
            return Results.Ok(entries);
        }).WithName("GetEntityAuditLog");

        group.MapGet("/audit/user/{userId:guid}", async (Guid userId, IObservabilityService service) =>
        {
            var entries = await service.GetUserAuditLogAsync(userId);
            return Results.Ok(entries);
        }).WithName("GetUserAuditLog");

        group.MapGet("/health/detailed", async (IObservabilityService service) =>
        {
            var results = await service.RunHealthChecksAsync();
            return Results.Ok(results);
        }).WithName("DetailedHealthCheck");

        group.MapPost("/metrics/{metricName}", async (string metricName, double value, IObservabilityService service) =>
        {
            await service.RecordMetricAsync(metricName, value);
            return Results.Ok();
        }).WithName("RecordMetric");

        group.MapGet("/metrics/{metricName}", async (string metricName, IObservabilityService service) =>
        {
            var metrics = await service.GetMetricsAsync(metricName);
            return Results.Ok(metrics);
        }).WithName("GetMetrics");
    }
}
