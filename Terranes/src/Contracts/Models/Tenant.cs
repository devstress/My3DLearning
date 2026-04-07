namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a tenant in the multi-tenant platform.
/// Each tenant has isolated data and user access.
/// </summary>
/// <param name="Id">Unique identifier for the tenant.</param>
/// <param name="Name">Display name of the tenant/organisation.</param>
/// <param name="Slug">URL-friendly short name.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the tenant was created.</param>
public sealed record Tenant(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
