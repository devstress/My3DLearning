using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class DesignEditorEndpoints
{
    public static void MapDesignEditorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/design-editor").WithTags("Design Editor");

        group.MapPost("/edits", async (DesignEdit edit, IDesignEditorService service) =>
        {
            var created = await service.ApplyEditAsync(edit);
            return Results.Created($"/api/design-editor/edits/{created.Id}", created);
        }).WithName("ApplyDesignEdit");

        group.MapGet("/placements/{sitePlacementId:guid}/history", async (Guid sitePlacementId, IDesignEditorService service) =>
        {
            var history = await service.GetEditHistoryAsync(sitePlacementId);
            return Results.Ok(history);
        }).WithName("GetEditHistory");

        group.MapPost("/placements/{sitePlacementId:guid}/undo", async (Guid sitePlacementId, IDesignEditorService service) =>
        {
            var undone = await service.UndoLastEditAsync(sitePlacementId);
            return undone is not null ? Results.Ok(undone) : Results.NotFound();
        }).WithName("UndoLastEdit");

        group.MapGet("/placements/{sitePlacementId:guid}/edits-by-type", async (Guid sitePlacementId, EditOperationType operation, IDesignEditorService service) =>
        {
            var edits = await service.GetEditsByTypeAsync(sitePlacementId, operation);
            return Results.Ok(edits);
        }).WithName("GetEditsByType");

        group.MapDelete("/placements/{sitePlacementId:guid}/reset", async (Guid sitePlacementId, IDesignEditorService service) =>
        {
            var count = await service.ResetEditsAsync(sitePlacementId);
            return Results.Ok(new { RemovedEdits = count });
        }).WithName("ResetEdits");
    }
}
