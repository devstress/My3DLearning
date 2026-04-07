using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a webhook registration for a partner to receive event callbacks.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="PartnerId">Partner who registered the webhook.</param>
/// <param name="CallbackUrl">URL to deliver events to.</param>
/// <param name="EventTopics">List of event topics to subscribe to.</param>
/// <param name="IsActive">Whether the webhook is currently active.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the webhook was registered.</param>
public sealed record WebhookRegistration(
    Guid Id,
    Guid PartnerId,
    string CallbackUrl,
    IReadOnlyList<string> EventTopics,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
