using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (PlatformUser user, string password, IAuthService service) =>
        {
            var created = await service.RegisterAsync(user, password);
            return Results.Created($"/api/auth/users/{created.Id}", created);
        }).WithName("RegisterUser");

        group.MapPost("/login", async (string email, string password, IAuthService service) =>
        {
            var user = await service.AuthenticateAsync(email, password);
            return user is not null ? Results.Ok(user) : Results.Unauthorized();
        }).WithName("LoginUser");

        group.MapGet("/users/{userId:guid}", async (Guid userId, IAuthService service) =>
        {
            var user = await service.GetUserAsync(userId);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        }).WithName("GetUser");

        group.MapPut("/users/{userId:guid}/role", async (Guid userId, UserRole newRole, IAuthService service) =>
        {
            var user = await service.UpdateRoleAsync(userId, newRole);
            return Results.Ok(user);
        }).WithName("UpdateUserRole");

        group.MapPost("/users/{userId:guid}/deactivate", async (Guid userId, IAuthService service) =>
        {
            var user = await service.DeactivateAsync(userId);
            return Results.Ok(user);
        }).WithName("DeactivateUser");
    }
}
