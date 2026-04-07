using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;

namespace Terranes.Platform.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapPost("/", async (Guid recipientId, NotificationType type, string title, string message, Guid? entityId, INotificationService service) =>
        {
            var notification = await service.SendAsync(recipientId, type, title, message, entityId);
            return Results.Created($"/api/notifications/{notification.Id}", notification);
        }).WithName("SendNotification");

        group.MapGet("/{notificationId:guid}", async (Guid notificationId, INotificationService service) =>
        {
            var notification = await service.GetNotificationAsync(notificationId);
            return notification is not null ? Results.Ok(notification) : Results.NotFound();
        }).WithName("GetNotification");

        group.MapGet("/recipient/{recipientId:guid}", async (Guid recipientId, INotificationService service) =>
        {
            var notifications = await service.GetNotificationsForRecipientAsync(recipientId);
            return Results.Ok(notifications);
        }).WithName("GetRecipientNotifications");

        group.MapGet("/recipient/{recipientId:guid}/unread", async (Guid recipientId, INotificationService service) =>
        {
            var notifications = await service.GetUnreadAsync(recipientId);
            return Results.Ok(notifications);
        }).WithName("GetUnreadNotifications");

        group.MapPut("/{notificationId:guid}/read", async (Guid notificationId, INotificationService service) =>
        {
            var notification = await service.MarkAsReadAsync(notificationId);
            return Results.Ok(notification);
        }).WithName("MarkNotificationRead");
    }
}
