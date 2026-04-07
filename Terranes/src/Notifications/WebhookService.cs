using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Notifications;

/// <summary>
/// In-memory implementation of <see cref="IWebhookService"/>.
/// Manages webhook registrations and simulated delivery for partner integrations.
/// </summary>
public sealed class WebhookService : IWebhookService
{
    private readonly ConcurrentDictionary<Guid, WebhookRegistration> _registrations = new();
    private readonly ConcurrentDictionary<Guid, List<WebhookDelivery>> _deliveries = new();
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(ILogger<WebhookService> logger) => _logger = logger;

    public Task<WebhookRegistration> RegisterAsync(Guid partnerId, string callbackUrl, IReadOnlyList<string> eventTopics, CancellationToken cancellationToken = default)
    {
        if (partnerId == Guid.Empty)
            throw new ArgumentException("Partner ID is required.", nameof(partnerId));

        if (string.IsNullOrWhiteSpace(callbackUrl))
            throw new ArgumentException("Callback URL is required.", nameof(callbackUrl));

        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Callback URL must be a valid absolute URI.", nameof(callbackUrl));

        if (eventTopics is null || eventTopics.Count == 0)
            throw new ArgumentException("At least one event topic is required.", nameof(eventTopics));

        var registration = new WebhookRegistration(
            Id: Guid.NewGuid(),
            PartnerId: partnerId,
            CallbackUrl: callbackUrl,
            EventTopics: eventTopics,
            IsActive: true,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        if (!_registrations.TryAdd(registration.Id, registration))
            throw new InvalidOperationException("Webhook ID conflict.");

        _deliveries.TryAdd(registration.Id, []);

        _logger.LogInformation("Registered webhook {WebhookId} for partner {PartnerId}", registration.Id, partnerId);
        return Task.FromResult(registration);
    }

    public Task<WebhookRegistration?> GetRegistrationAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        _registrations.TryGetValue(webhookId, out var registration);
        return Task.FromResult(registration);
    }

    public Task<IReadOnlyList<WebhookRegistration>> GetPartnerWebhooksAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebhookRegistration> result = _registrations.Values
            .Where(r => r.PartnerId == partnerId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<WebhookRegistration> DeactivateAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        if (!_registrations.TryGetValue(webhookId, out var existing))
            throw new InvalidOperationException($"Webhook {webhookId} not found.");

        var updated = existing with { IsActive = false };
        _registrations[webhookId] = updated;

        _logger.LogInformation("Deactivated webhook {WebhookId}", webhookId);
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<WebhookDelivery>> DeliverEventAsync(PlatformEvent platformEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(platformEvent);

        var matchingWebhooks = _registrations.Values
            .Where(r => r.IsActive && r.EventTopics.Any(t => string.Equals(t, platformEvent.Topic, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var deliveries = new List<WebhookDelivery>();

        foreach (var webhook in matchingWebhooks)
        {
            var delivery = new WebhookDelivery(
                Id: Guid.NewGuid(),
                WebhookId: webhook.Id,
                EventId: platformEvent.Id,
                Status: WebhookDeliveryStatus.Delivered,
                AttemptCount: 1,
                LastAttemptAtUtc: DateTimeOffset.UtcNow);

            if (_deliveries.TryGetValue(webhook.Id, out var history))
                history.Add(delivery);

            deliveries.Add(delivery);
        }

        _logger.LogInformation("Delivered event {EventId} to {Count} webhooks", platformEvent.Id, deliveries.Count);
        return Task.FromResult<IReadOnlyList<WebhookDelivery>>(deliveries);
    }

    public Task<IReadOnlyList<WebhookDelivery>> GetDeliveryHistoryAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebhookDelivery> result = _deliveries.TryGetValue(webhookId, out var history)
            ? history.OrderByDescending(d => d.LastAttemptAtUtc).ToList()
            : [];
        return Task.FromResult(result);
    }
}
