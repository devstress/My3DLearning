using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for authentication and authorization — user registration, login, role-based access.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user.</summary>
    Task<PlatformUser> RegisterAsync(PlatformUser user, string password, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user with email and password.</summary>
    Task<PlatformUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by ID.</summary>
    Task<PlatformUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Updates a user's role.</summary>
    Task<PlatformUser> UpdateRoleAsync(Guid userId, UserRole newRole, CancellationToken cancellationToken = default);

    /// <summary>Deactivates a user account.</summary>
    Task<PlatformUser> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Checks if a user has a specific role.</summary>
    Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
}
