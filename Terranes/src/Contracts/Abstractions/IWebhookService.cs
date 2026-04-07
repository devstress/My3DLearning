using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Manages webhook registrations and delivery tracking for partner integrations.
/// </summary>
public interface IWebhookService
{
    /// <summary>Registers a webhook for a partner.</summary>
    Task<WebhookRegistration> RegisterAsync(Guid partnerId, string callbackUrl, IReadOnlyList<string> eventTopics, CancellationToken cancellationToken = default);

    /// <summary>Gets a webhook registration by ID.</summary>
    Task<WebhookRegistration?> GetRegistrationAsync(Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>Gets all webhooks registered by a partner.</summary>
    Task<IReadOnlyList<WebhookRegistration>> GetPartnerWebhooksAsync(Guid partnerId, CancellationToken cancellationToken = default);

    /// <summary>Deactivates a webhook registration.</summary>
    Task<WebhookRegistration> DeactivateAsync(Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>Simulates delivering an event to all matching webhooks.</summary>
    Task<IReadOnlyList<WebhookDelivery>> DeliverEventAsync(PlatformEvent platformEvent, CancellationToken cancellationToken = default);

    /// <summary>Gets delivery history for a webhook.</summary>
    Task<IReadOnlyList<WebhookDelivery>> GetDeliveryHistoryAsync(Guid webhookId, CancellationToken cancellationToken = default);
}
