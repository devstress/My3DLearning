using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="ITenantService"/>.
/// Manages multi-tenant isolation with tenant creation, lookup, and user assignment.
/// </summary>
public sealed class TenantService : ITenantService
{
    private readonly ConcurrentDictionary<Guid, Tenant> _tenants = new();
    private readonly ConcurrentDictionary<string, Guid> _slugIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly IAuthService _authService;
    private readonly ILogger<TenantService> _logger;

    // Track user→tenant mapping for GetTenantUsersAsync
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Guid>> _tenantUsers = new();

    public TenantService(IAuthService authService, ILogger<TenantService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (string.IsNullOrWhiteSpace(tenant.Name))
            throw new ArgumentException("Tenant name is required.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Slug))
            throw new ArgumentException("Tenant slug is required.", nameof(tenant));

        if (tenant.Slug.Length < 3 || tenant.Slug.Length > 50)
            throw new ArgumentException("Slug must be between 3 and 50 characters.", nameof(tenant));

        if (!_slugIndex.TryAdd(tenant.Slug, tenant.Id == Guid.Empty ? Guid.NewGuid() : tenant.Id))
            throw new InvalidOperationException($"Slug '{tenant.Slug}' is already taken.");

        var id = _slugIndex[tenant.Slug];
        var persisted = tenant with { Id = id, IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow };

        if (!_tenants.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Tenant {persisted.Id} already exists.");

        _tenantUsers[persisted.Id] = [];

        _logger.LogInformation("Created tenant {TenantId}", persisted.Id);
        return Task.FromResult(persisted);
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<Tenant?>(null);

        if (!_slugIndex.TryGetValue(slug, out var tenantId))
            return Task.FromResult<Tenant?>(null);

        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<IReadOnlyList<Tenant>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Tenant> result = _tenants.Values
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Tenant> DeactivateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!_tenants.TryGetValue(tenantId, out var tenant))
            throw new InvalidOperationException($"Tenant {tenantId} not found.");

        var updated = tenant with { IsActive = false };
        _tenants[tenantId] = updated;

        _logger.LogInformation("Deactivated tenant {TenantId}", tenantId);
        return Task.FromResult(updated);
    }

    public async Task<IReadOnlyList<PlatformUser>> GetTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!_tenants.ContainsKey(tenantId))
            throw new InvalidOperationException($"Tenant {tenantId} not found.");

        if (!_tenantUsers.TryGetValue(tenantId, out var userIds))
            return [];

        var users = new List<PlatformUser>();
        foreach (var userId in userIds)
        {
            var user = await _authService.GetUserAsync(userId, cancellationToken);
            if (user is not null && user.TenantId == tenantId)
                users.Add(user);
        }

        return users;
    }
}
