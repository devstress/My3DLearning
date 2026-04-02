using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ITenantOnboardingService"/>.
/// Orchestrates the full provisioning workflow: validates the request, creates a
/// quota, provisions a broker namespace, and tracks lifecycle status.
/// </summary>
public sealed class InMemoryTenantOnboardingService : ITenantOnboardingService
{
    private readonly ConcurrentDictionary<string, TenantOnboardingResult> _tenants = new(StringComparer.Ordinal);
    private readonly ITenantQuotaManager _quotaManager;
    private readonly IBrokerNamespaceProvisioner _namespaceProvisioner;
    private readonly ILogger<InMemoryTenantOnboardingService> _logger;

    /// <summary>Initialises a new instance of <see cref="InMemoryTenantOnboardingService"/>.</summary>
    public InMemoryTenantOnboardingService(
        ITenantQuotaManager quotaManager,
        IBrokerNamespaceProvisioner namespaceProvisioner,
        ILogger<InMemoryTenantOnboardingService> logger)
    {
        ArgumentNullException.ThrowIfNull(quotaManager);
        ArgumentNullException.ThrowIfNull(namespaceProvisioner);
        ArgumentNullException.ThrowIfNull(logger);

        _quotaManager = quotaManager;
        _namespaceProvisioner = namespaceProvisioner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TenantOnboardingResult> ProvisionAsync(
        TenantOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AdminEmail);

        if (_tenants.ContainsKey(request.TenantId))
        {
            throw new InvalidOperationException($"Tenant '{request.TenantId}' is already onboarded.");
        }

        // Mark as provisioning
        var pending = new TenantOnboardingResult(
            TenantId: request.TenantId,
            Status: OnboardingStatus.Provisioning,
            ProvisionedAt: null,
            NamespaceConfig: null,
            Quota: null);

        _tenants[request.TenantId] = pending;
        _logger.LogInformation("Provisioning started for tenant {TenantId} (plan={Plan})", request.TenantId, request.Plan);

        try
        {
            // 1. Assign default quota based on plan
            var quota = InMemoryTenantQuotaManager.GetDefaultQuota(request.Plan);
            await _quotaManager.SetQuotaAsync(request.TenantId, quota, cancellationToken).ConfigureAwait(false);

            // 2. Provision broker namespace
            var nsConfig = InMemoryBrokerNamespaceProvisioner.BuildDefaultConfig(request.TenantId, request.Plan);
            var provisioned = await _namespaceProvisioner
                .ProvisionNamespaceAsync(request.TenantId, nsConfig, cancellationToken)
                .ConfigureAwait(false);

            // 3. Mark active
            var result = new TenantOnboardingResult(
                TenantId: request.TenantId,
                Status: OnboardingStatus.Active,
                ProvisionedAt: DateTimeOffset.UtcNow,
                NamespaceConfig: provisioned,
                Quota: quota);

            _tenants[request.TenantId] = result;
            _logger.LogInformation("Tenant {TenantId} onboarded successfully", request.TenantId);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failed = new TenantOnboardingResult(
                TenantId: request.TenantId,
                Status: OnboardingStatus.Failed,
                ProvisionedAt: null,
                NamespaceConfig: null,
                Quota: null,
                ErrorMessage: ex.Message);

            _tenants[request.TenantId] = failed;
            _logger.LogError(ex, "Provisioning failed for tenant {TenantId}", request.TenantId);
            return failed;
        }
    }

    /// <inheritdoc />
    public async Task<TenantOnboardingResult> DeprovisionAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!_tenants.TryGetValue(tenantId, out var existing))
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found.");
        }

        if (existing.Status == OnboardingStatus.Deprovisioned)
        {
            return existing;
        }

        _logger.LogInformation("Deprovisioning tenant {TenantId}", tenantId);

        await _namespaceProvisioner.DeprovisionNamespaceAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var deprovisioned = new TenantOnboardingResult(
            TenantId: tenantId,
            Status: OnboardingStatus.Deprovisioned,
            ProvisionedAt: existing.ProvisionedAt,
            NamespaceConfig: null,
            Quota: null);

        _tenants[tenantId] = deprovisioned;
        _logger.LogInformation("Tenant {TenantId} deprovisioned", tenantId);
        return deprovisioned;
    }

    /// <inheritdoc />
    public Task<TenantOnboardingResult?> GetStatusAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _tenants.TryGetValue(tenantId, out var result);
        return Task.FromResult(result);
    }
}
