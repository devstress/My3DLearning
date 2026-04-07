using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Infrastructure;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class AuthServiceTests
{
    private AuthService _sut = null!;
    private Guid _tenantId;

    [SetUp]
    public void SetUp()
    {
        _sut = new AuthService(NullLogger<AuthService>.Instance);
        _tenantId = Guid.NewGuid();
    }

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidUser_ReturnsActiveUser()
    {
        var user = MakeUser();
        var registered = await _sut.RegisterAsync(user, "SecureP@ss1");

        Assert.That(registered.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.IsActive, Is.True);
        Assert.That(registered.Email, Is.EqualTo("jane@example.com"));
    }

    [Test]
    public void RegisterAsync_EmptyEmail_ThrowsArgumentException()
    {
        var user = MakeUser() with { Email = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(user, "SecureP@ss1"));
    }

    [Test]
    public void RegisterAsync_ShortPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(MakeUser(), "short"));
    }

    [Test]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(MakeUser(), "SecureP@ss2"));
    }

    [Test]
    public void RegisterAsync_EmptyTenantId_ThrowsArgumentException()
    {
        var user = MakeUser() with { TenantId = Guid.Empty };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(user, "SecureP@ss1"));
    }

    // ── 2. Authentication ──

    [Test]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
    {
        await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var result = await _sut.AuthenticateAsync("jane@example.com", "SecureP@ss1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LastLoginAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task AuthenticateAsync_WrongPassword_ReturnsNull()
    {
        await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var result = await _sut.AuthenticateAsync("jane@example.com", "WrongPassword");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_NonExistentEmail_ReturnsNull()
    {
        var result = await _sut.AuthenticateAsync("nobody@example.com", "SecureP@ss1");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_DeactivatedUser_ReturnsNull()
    {
        var registered = await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        await _sut.DeactivateAsync(registered.Id);

        var result = await _sut.AuthenticateAsync("jane@example.com", "SecureP@ss1");
        Assert.That(result, Is.Null);
    }

    // ── 3. Role Management ──

    [Test]
    public async Task UpdateRoleAsync_ValidUser_UpdatesRole()
    {
        var registered = await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var updated = await _sut.UpdateRoleAsync(registered.Id, UserRole.Admin);

        Assert.That(updated.Role, Is.EqualTo(UserRole.Admin));
    }

    [Test]
    public async Task HasRoleAsync_CorrectRole_ReturnsTrue()
    {
        var registered = await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var result = await _sut.HasRoleAsync(registered.Id, UserRole.Buyer);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasRoleAsync_WrongRole_ReturnsFalse()
    {
        var registered = await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var result = await _sut.HasRoleAsync(registered.Id, UserRole.Admin);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeactivateAsync_ActiveUser_SetsInactive()
    {
        var registered = await _sut.RegisterAsync(MakeUser(), "SecureP@ss1");
        var deactivated = await _sut.DeactivateAsync(registered.Id);

        Assert.That(deactivated.IsActive, Is.False);
    }

    [Test]
    public void DeactivateAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }

    private PlatformUser MakeUser() => new(
        Guid.Empty, "jane@example.com", "Jane Smith", UserRole.Buyer,
        _tenantId, false, default, null);
}
