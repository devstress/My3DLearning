using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

        group.MapPost("/", async (RegisterWebhookRequest request, IWebhookService service) =>
        {
            var registration = await service.RegisterAsync(request.PartnerId, request.CallbackUrl, request.EventTopics);
            return Results.Created($"/api/webhooks/{registration.Id}", registration);
        }).WithName("RegisterWebhook");

        group.MapGet("/{webhookId:guid}", async (Guid webhookId, IWebhookService service) =>
        {
            var registration = await service.GetRegistrationAsync(webhookId);
            return registration is not null ? Results.Ok(registration) : Results.NotFound();
        }).WithName("GetWebhook");

        group.MapGet("/partner/{partnerId:guid}", async (Guid partnerId, IWebhookService service) =>
        {
            var webhooks = await service.GetPartnerWebhooksAsync(partnerId);
            return Results.Ok(webhooks);
        }).WithName("GetPartnerWebhooks");

        group.MapPost("/{webhookId:guid}/deactivate", async (Guid webhookId, IWebhookService service) =>
        {
            var registration = await service.DeactivateAsync(webhookId);
            return Results.Ok(registration);
        }).WithName("DeactivateWebhook");

        group.MapGet("/{webhookId:guid}/deliveries", async (Guid webhookId, IWebhookService service) =>
        {
            var deliveries = await service.GetDeliveryHistoryAsync(webhookId);
            return Results.Ok(deliveries);
        }).WithName("GetWebhookDeliveries");
    }
}

public sealed record RegisterWebhookRequest(Guid PartnerId, string CallbackUrl, List<string> EventTopics);