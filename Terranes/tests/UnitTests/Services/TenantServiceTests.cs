using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Infrastructure;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class TenantServiceTests
{
    private AuthService _authService = null!;
    private TenantService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _authService = new AuthService(NullLogger<AuthService>.Instance);
        _sut = new TenantService(_authService, NullLogger<TenantService>.Instance);
    }

    // ── 1. Tenant Creation ──

    [Test]
    public async Task CreateAsync_ValidTenant_ReturnsActiveTenant()
    {
        var tenant = MakeTenant();
        var created = await _sut.CreateAsync(tenant);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.IsActive, Is.True);
        Assert.That(created.Slug, Is.EqualTo("acme-corp"));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var tenant = MakeTenant() with { Name = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(tenant));
    }

    [Test]
    public void CreateAsync_ShortSlug_ThrowsArgumentException()
    {
        var tenant = MakeTenant() with { Slug = "ab" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(tenant));
    }

    [Test]
    public async Task CreateAsync_DuplicateSlug_ThrowsInvalidOperationException()
    {
        await _sut.CreateAsync(MakeTenant());
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(MakeTenant()));
    }

    // ── 2. Lookup ──

    [Test]
    public async Task GetBySlugAsync_ExistingSlug_ReturnsTenant()
    {
        await _sut.CreateAsync(MakeTenant());
        var found = await _sut.GetBySlugAsync("acme-corp");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("ACME Corporation"));
    }

    [Test]
    public async Task GetBySlugAsync_NonExistentSlug_ReturnsNull()
    {
        var result = await _sut.GetBySlugAsync("nonexistent");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_ExistingTenant_ReturnsTenant()
    {
        var created = await _sut.CreateAsync(MakeTenant());
        var found = await _sut.GetByIdAsync(created.Id);

        Assert.That(found, Is.Not.Null);
    }

    // ── 3. Lifecycle ──

    [Test]
    public async Task DeactivateAsync_ActiveTenant_SetsInactive()
    {
        var created = await _sut.CreateAsync(MakeTenant());
        var deactivated = await _sut.DeactivateAsync(created.Id);

        Assert.That(deactivated.IsActive, Is.False);
    }

    [Test]
    public void DeactivateAsync_NonExistentTenant_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task ListActiveAsync_ReturnsOnlyActive()
    {
        var t1 = await _sut.CreateAsync(MakeTenant() with { Slug = "tenant-a", Name = "A" });
        await _sut.CreateAsync(MakeTenant() with { Slug = "tenant-b", Name = "B" });
        await _sut.DeactivateAsync(t1.Id);

        var active = await _sut.ListActiveAsync();
        Assert.That(active, Has.Count.EqualTo(1));
        Assert.That(active[0].Name, Is.EqualTo("B"));
    }

    [Test]
    public async Task GetTenantUsersAsync_EmptyTenant_ReturnsEmpty()
    {
        var created = await _sut.CreateAsync(MakeTenant());
        var users = await _sut.GetTenantUsersAsync(created.Id);

        Assert.That(users, Is.Empty);
    }

    [Test]
    public void GetTenantUsersAsync_NonExistentTenant_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetTenantUsersAsync(Guid.NewGuid()));
    }

    private static Tenant MakeTenant() => new(
        Guid.Empty, "ACME Corporation", "acme-corp", false, default);
}
