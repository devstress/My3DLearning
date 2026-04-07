using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a partner in the Terranes ecosystem (builder, landscaper, furniture supplier, etc.).
/// </summary>
/// <param name="Id">Unique identifier for this partner.</param>
/// <param name="BusinessName">Registered business name of the partner.</param>
/// <param name="Category">Primary partner category.</param>
/// <param name="ContactEmail">Primary contact email address.</param>
/// <param name="ContactPhone">Primary contact phone number.</param>
/// <param name="ServiceRegions">States or regions where the partner operates (e.g. ["NSW", "VIC"]).</param>
/// <param name="IsActive">Whether the partner is currently active and accepting quote requests.</param>
/// <param name="RegisteredAtUtc">UTC timestamp when the partner registered on the platform.</param>
public sealed record Partner(
    Guid Id,
    string BusinessName,
    PartnerCategory Category,
    string ContactEmail,
    string ContactPhone,
    IReadOnlyList<string> ServiceRegions,
    bool IsActive,
    DateTimeOffset RegisteredAtUtc);
