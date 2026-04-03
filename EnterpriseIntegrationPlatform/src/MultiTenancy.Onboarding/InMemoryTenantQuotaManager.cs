using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ITenantQuotaManager"/>.
/// Provides default quotas per <see cref="TenantPlan"/> tier and enforces
/// resource usage limits at runtime.
/// </summary>
public sealed class InMemoryTenantQuotaManager : ITenantQuotaManager
{
    private readonly ConcurrentDictionary<string, TenantQuota> _quotas = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryTenantQuotaManager> _logger;

    /// <summary>Initialises a new instance of <see cref="InMemoryTenantQuotaManager"/>.</summary>
    public InMemoryTenantQuotaManager(ILogger<InMemoryTenantQuotaManager> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<TenantQuota?> GetQuotaAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _quotas.TryGetValue(tenantId, out var quota);
        return Task.FromResult(quota);
    }

    /// <inheritdoc />
    public Task SetQuotaAsync(string tenantId, TenantQuota quota, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(quota);

        _quotas[tenantId] = quota;
        _logger.LogInformation("Quota updated for tenant {TenantId}: {Quota}", tenantId, quota);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> EnforceQuotaAsync(
        string tenantId,
        string resourceType,
        long usage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        if (!_quotas.TryGetValue(tenantId, out var quota))
        {
            _logger.LogWarning("No quota found for tenant {TenantId}; denying resource {ResourceType}", tenantId, resourceType);
            return Task.FromResult(false);
        }

        var allowed = resourceType.ToUpperInvariant() switch
        {
            "MESSAGES" => usage <= quota.MaxMessagesPerDay,
            "MESSAGESIZE" => usage <= quota.MaxMessageSizeBytes,
            "QUEUES" => usage <= quota.MaxQueues,
            "CONNECTORS" => usage <= quota.MaxConnectors,
            "RETENTION" => usage <= quota.MaxRetentionDays,
            "STORAGE" => usage <= quota.StorageLimitMb,
            _ => false,
        };

        if (!allowed)
        {
            _logger.LogWarning(
                "Quota exceeded for tenant {TenantId}, resource {ResourceType}: usage {Usage}",
                tenantId, resourceType, usage);
        }

        return Task.FromResult(allowed);
    }

    /// <summary>
    /// Returns the default <see cref="TenantQuota"/> for the specified plan tier.
    /// </summary>
    /// <param name="plan">The subscription plan.</param>
    /// <returns>A quota with sensible defaults for the tier.</returns>
    public static TenantQuota GetDefaultQuota(TenantPlan plan) => plan switch
    {
        TenantPlan.Free => new TenantQuota(
            MaxMessagesPerDay: 1_000,
            MaxMessageSizeBytes: 64 * 1024,
            MaxQueues: 5,
            MaxConnectors: 2,
            MaxRetentionDays: 1,
            StorageLimitMb: 100),
        TenantPlan.Standard => new TenantQuota(
            MaxMessagesPerDay: 100_000,
            MaxMessageSizeBytes: 256 * 1024,
            MaxQueues: 25,
            MaxConnectors: 10,
            MaxRetentionDays: 7,
            StorageLimitMb: 1_024),
        TenantPlan.Premium => new TenantQuota(
            MaxMessagesPerDay: 1_000_000,
            MaxMessageSizeBytes: 1024 * 1024,
            MaxQueues: 100,
            MaxConnectors: 50,
            MaxRetentionDays: 30,
            StorageLimitMb: 10_240),
        TenantPlan.Enterprise => new TenantQuota(
            MaxMessagesPerDay: 10_000_000,
            MaxMessageSizeBytes: 10 * 1024 * 1024,
            MaxQueues: 500,
            MaxConnectors: 200,
            MaxRetentionDays: 90,
            StorageLimitMb: 102_400),
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown tenant plan."),
    };
}
