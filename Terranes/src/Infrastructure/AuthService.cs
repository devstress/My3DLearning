using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IAuthService"/>.
/// Manages user registration, authentication with hashed passwords, and role-based access.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly ConcurrentDictionary<Guid, PlatformUser> _users = new();
    private readonly ConcurrentDictionary<Guid, string> _passwordHashes = new();
    private readonly ConcurrentDictionary<string, Guid> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AuthService> _logger;

    public AuthService(ILogger<AuthService> logger) => _logger = logger;

    public Task<PlatformUser> RegisterAsync(PlatformUser user, string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email is required.", nameof(user));

        if (string.IsNullOrWhiteSpace(user.DisplayName))
            throw new ArgumentException("Display name is required.", nameof(user));

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.", nameof(password));

        if (user.TenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(user));

        if (!_emailIndex.TryAdd(user.Email, user.Id == Guid.Empty ? Guid.NewGuid() : user.Id))
            throw new InvalidOperationException($"Email {user.Email} is already registered.");

        var id = _emailIndex[user.Email];
        var persisted = user with { Id = id, IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow };

        if (!_users.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"User {persisted.Id} already exists.");

        _passwordHashes[persisted.Id] = HashPassword(password);

        _logger.LogInformation("Registered user {UserId} ({Role})", persisted.Id, persisted.Role);
        return Task.FromResult(persisted);
    }

    public Task<PlatformUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult<PlatformUser?>(null);

        if (!_emailIndex.TryGetValue(email, out var userId))
            return Task.FromResult<PlatformUser?>(null);

        if (!_users.TryGetValue(userId, out var user) || !user.IsActive)
            return Task.FromResult<PlatformUser?>(null);

        if (!_passwordHashes.TryGetValue(userId, out var hash) || hash != HashPassword(password))
            return Task.FromResult<PlatformUser?>(null);

        var updated = user with { LastLoginAtUtc = DateTimeOffset.UtcNow };
        _users[userId] = updated;

        _logger.LogInformation("User {UserId} authenticated", userId);
        return Task.FromResult<PlatformUser?>(updated);
    }

    public Task<PlatformUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<PlatformUser> UpdateRoleAsync(Guid userId, UserRole newRole, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
            throw new InvalidOperationException($"User {userId} not found.");

        var updated = user with { Role = newRole };
        _users[userId] = updated;

        _logger.LogInformation("Updated user {UserId} role to {Role}", userId, newRole);
        return Task.FromResult(updated);
    }

    public Task<PlatformUser> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
            throw new InvalidOperationException($"User {userId} not found.");

        var updated = user with { IsActive = false };
        _users[userId] = updated;

        _logger.LogInformation("Deactivated user {UserId}", userId);
        return Task.FromResult(updated);
    }

    public Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
            return Task.FromResult(false);

        return Task.FromResult(user.Role == role && user.IsActive);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
