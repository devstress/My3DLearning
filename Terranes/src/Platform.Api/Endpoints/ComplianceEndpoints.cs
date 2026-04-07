using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class ComplianceEndpoints
{
    public static void MapComplianceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/compliance").WithTags("Compliance");

        group.MapPost("/check", async (ComplianceCheckRequest request, IComplianceService service) =>
        {
            var result = await service.CheckAsync(request.SitePlacementId, request.Jurisdiction);
            return Results.Ok(result);
        }).WithName("RunComplianceCheck");

        group.MapGet("/{id:guid}", async (Guid id, IComplianceService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }).WithName("GetComplianceResult");

        group.MapGet("/placement/{sitePlacementId:guid}", async (Guid sitePlacementId, IComplianceService service) =>
        {
            var results = await service.GetBySitePlacementAsync(sitePlacementId);
            return Results.Ok(results);
        }).WithName("GetComplianceByPlacement");
    }
}

/// <summary>
/// Request body for running a compliance check.
/// </summary>
public sealed record ComplianceCheckRequest(Guid SitePlacementId, string Jurisdiction);
