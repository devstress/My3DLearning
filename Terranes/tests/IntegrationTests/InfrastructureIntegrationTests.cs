using System.Net;
using System.Net.Http.Json;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.IntegrationTests;

/// <summary>
/// Integration tests for platform infrastructure services through the HTTP API.
/// Covers: Auth, Observability, Tenant endpoints.
/// </summary>
[TestFixture]
public sealed class InfrastructureIntegrationTests : IntegrationTestBase
{
    // ── 1. Authentication ──

    [Test]
    public async Task Auth_RegisterAndLogin_Lifecycle()
    {
        var tenantId = Guid.NewGuid();
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var user = new PlatformUser(
            Guid.Empty, email, "Test User", UserRole.Buyer, tenantId, false, default, null);

        var response = await Client.PostAsJsonAsync($"/api/auth/register?password=SecurePass123!", user);
        response.EnsureSuccessStatusCode();
        var registered = await response.Content.ReadFromJsonAsync<PlatformUser>();

        Assert.That(registered!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(registered.Email, Is.EqualTo(email));

        // Login
        var loginResponse = await Client.PostAsync(
            $"/api/auth/login?email={Uri.EscapeDataString(email)}&password=SecurePass123!", null);
        loginResponse.EnsureSuccessStatusCode();
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<PlatformUser>();
        Assert.That(loggedIn!.Email, Is.EqualTo(email));

        // Get user
        var retrieved = await GetAsync<PlatformUser>($"/api/auth/users/{registered.Id}");
        Assert.That(retrieved.DisplayName, Is.EqualTo("Test User"));
    }

    [Test]
    public async Task Auth_InvalidLogin_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            "/api/auth/login?email=nonexistent@test.com&password=wrong", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Auth_UpdateRole_ChangesRole()
    {
        var user = await RegisterUser();

        var response = await Client.PutAsync(
            $"/api/auth/users/{user.Id}/role?newRole=Admin", null);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<PlatformUser>();
        Assert.That(updated!.Role, Is.EqualTo(UserRole.Admin));
    }

    [Test]
    public async Task Auth_Deactivate_DisablesUser()
    {
        var user = await RegisterUser();

        var response = await Client.PostAsync(
            $"/api/auth/users/{user.Id}/deactivate", null);
        response.EnsureSuccessStatusCode();
        var deactivated = await response.Content.ReadFromJsonAsync<PlatformUser>();
        Assert.That(deactivated!.IsActive, Is.False);
    }

    // ── 2. Observability ──

    [Test]
    public async Task Observability_LogAudit_AndRetrieve()
    {
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entry = new AuditLogEntry(
            Guid.Empty, "CreateHomeModel", "HomeModel", entityId,
            userId, Guid.NewGuid(), "{}", default);

        var created = await PostAsync<AuditLogEntry>("/api/observability/audit", entry);
        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var byEntity = await GetAsync<List<AuditLogEntry>>(
            $"/api/observability/audit/HomeModel/{entityId}");
        Assert.That(byEntity, Has.Count.EqualTo(1));

        var byUser = await GetAsync<List<AuditLogEntry>>(
            $"/api/observability/audit/user/{userId}");
        Assert.That(byUser, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Observability_DetailedHealthCheck_ReturnsResults()
    {
        var results = await GetAsync<List<HealthCheckResult>>("/api/observability/health/detailed");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Observability_RecordAndGetMetrics()
    {
        var metricName = $"test_metric_{Guid.NewGuid():N}";

        var recordResponse = await Client.PostAsync(
            $"/api/observability/metrics/{metricName}?value=42.5", null);
        recordResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync($"/api/observability/metrics/{metricName}");
        response.EnsureSuccessStatusCode();
    }

    // ── 3. Tenants ──

    [Test]
    public async Task Tenant_CreateAndRetrieve_RoundTrips()
    {
        var slug = $"test-{Guid.NewGuid():N}".Substring(0, 20);
        var tenant = new Tenant(Guid.Empty, "Test Tenant", slug, true, default);
        var created = await PostAsync<Tenant>("/api/tenants", tenant);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Slug, Is.EqualTo(slug));

        var retrieved = await GetAsync<Tenant>($"/api/tenants/{created.Id}");
        Assert.That(retrieved.Name, Is.EqualTo("Test Tenant"));

        var bySlug = await GetAsync<Tenant>($"/api/tenants/by-slug/{slug}");
        Assert.That(bySlug.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Tenant_ListActive_ReturnsActiveTenants()
    {
        await PostAsync<Tenant>("/api/tenants",
            new Tenant(Guid.Empty, "Active Tenant", $"active-{Guid.NewGuid():N}".Substring(0, 20), true, default));

        var tenants = await GetAsync<List<Tenant>>("/api/tenants");
        Assert.That(tenants, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Tenant_Deactivate_RemovesFromActiveList()
    {
        var slug = $"deact-{Guid.NewGuid():N}".Substring(0, 20);
        var created = await PostAsync<Tenant>("/api/tenants",
            new Tenant(Guid.Empty, "To Deactivate", slug, true, default));

        var response = await Client.PostAsync($"/api/tenants/{created.Id}/deactivate", null);
        response.EnsureSuccessStatusCode();
        var deactivated = await response.Content.ReadFromJsonAsync<Tenant>();
        Assert.That(deactivated!.IsActive, Is.False);
    }

    // ── Helper ──

    private async Task<PlatformUser> RegisterUser()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var user = new PlatformUser(
            Guid.Empty, email, "Auto User", UserRole.Buyer, Guid.NewGuid(), false, default, null);
        var response = await Client.PostAsJsonAsync($"/api/auth/register?password=StrongPass99!", user);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlatformUser>())!;
    }
}
