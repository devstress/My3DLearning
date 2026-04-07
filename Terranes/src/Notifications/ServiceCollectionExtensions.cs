using Microsoft.Extensions.DependencyInjection;
using Terranes.Contracts.Abstractions;

namespace Terranes.Notifications;

/// <summary>
/// Registers all Notification &amp; Events services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IEventBusService, EventBusService>();
        services.AddSingleton<IWebhookService, WebhookService>();
        return services;
    }
}
