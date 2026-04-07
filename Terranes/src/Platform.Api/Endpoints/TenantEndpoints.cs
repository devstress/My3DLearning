using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Multi-Tenancy");

        group.MapPost("/", async (Tenant tenant, ITenantService service) =>
        {
            var created = await service.CreateAsync(tenant);
            return Results.Created($"/api/tenants/{created.Id}", created);
        }).WithName("CreateTenant");

        group.MapGet("/{tenantId:guid}", async (Guid tenantId, ITenantService service) =>
        {
            var tenant = await service.GetByIdAsync(tenantId);
            return tenant is not null ? Results.Ok(tenant) : Results.NotFound();
        }).WithName("GetTenant");

        group.MapGet("/by-slug/{slug}", async (string slug, ITenantService service) =>
        {
            var tenant = await service.GetBySlugAsync(slug);
            return tenant is not null ? Results.Ok(tenant) : Results.NotFound();
        }).WithName("GetTenantBySlug");

        group.MapGet("/", async (ITenantService service) =>
        {
            var tenants = await service.ListActiveAsync();
            return Results.Ok(tenants);
        }).WithName("ListActiveTenants");

        group.MapPost("/{tenantId:guid}/deactivate", async (Guid tenantId, ITenantService service) =>
        {
            var tenant = await service.DeactivateAsync(tenantId);
            return Results.Ok(tenant);
        }).WithName("DeactivateTenant");

        group.MapGet("/{tenantId:guid}/users", async (Guid tenantId, ITenantService service) =>
        {
            var users = await service.GetTenantUsersAsync(tenantId);
            return Results.Ok(users);
        }).WithName("GetTenantUsers");
    }
}
