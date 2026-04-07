using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for multi-tenancy — tenant management and data isolation.
/// </summary>
public interface ITenantService
{
    /// <summary>Creates a new tenant.</summary>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a tenant by ID.</summary>
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a tenant by slug.</summary>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Lists all active tenants.</summary>
    Task<IReadOnlyList<Tenant>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Deactivates a tenant.</summary>
    Task<Tenant> DeactivateAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets all users for a tenant.</summary>
    Task<IReadOnlyList<PlatformUser>> GetTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
