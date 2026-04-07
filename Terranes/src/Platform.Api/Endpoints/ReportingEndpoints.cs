using Terranes.Contracts.Abstractions;

namespace Terranes.Platform.Api.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reporting");

        group.MapPost("/", async (string reportType, string title, Guid generatedByUserId, Guid tenantId, IReportingService service) =>
        {
            var report = await service.GenerateAsync(reportType, title, generatedByUserId, tenantId);
            return Results.Created($"/api/reports/{report.Id}", report);
        }).WithName("GenerateReport");

        group.MapGet("/{reportId:guid}", async (Guid reportId, IReportingService service) =>
        {
            var report = await service.GetReportAsync(reportId);
            return report is not null ? Results.Ok(report) : Results.NotFound();
        }).WithName("GetReport");

        group.MapGet("/tenant/{tenantId:guid}", async (Guid tenantId, IReportingService service) =>
        {
            var reports = await service.GetTenantReportsAsync(tenantId);
            return Results.Ok(reports);
        }).WithName("GetTenantReports");

        group.MapGet("/types", async (IReportingService service) =>
        {
            var types = await service.GetAvailableReportTypesAsync();
            return Results.Ok(types);
        }).WithName("GetReportTypes");
    }
}
