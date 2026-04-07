using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a platform user with authentication and role information.
/// </summary>
/// <param name="Id">Unique identifier for the user.</param>
/// <param name="Email">User's email address (used for login).</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Role">User role for RBAC.</param>
/// <param name="TenantId">Tenant the user belongs to.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the user was created.</param>
/// <param name="LastLoginAtUtc">UTC timestamp of the last login, or null.</param>
public sealed record PlatformUser(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    Guid TenantId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);
