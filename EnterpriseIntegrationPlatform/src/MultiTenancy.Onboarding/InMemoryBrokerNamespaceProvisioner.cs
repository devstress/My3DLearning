using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IBrokerNamespaceProvisioner"/>.
/// Generates deterministic namespace naming conventions based on tenant identifiers
/// and stores configurations in a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemoryBrokerNamespaceProvisioner : IBrokerNamespaceProvisioner
{
    private readonly ConcurrentDictionary<string, BrokerNamespaceConfig> _namespaces = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryBrokerNamespaceProvisioner> _logger;

    /// <summary>Initialises a new instance of <see cref="InMemoryBrokerNamespaceProvisioner"/>.</summary>
    public InMemoryBrokerNamespaceProvisioner(ILogger<InMemoryBrokerNamespaceProvisioner> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<BrokerNamespaceConfig> ProvisionNamespaceAsync(
        string tenantId,
        BrokerNamespaceConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);

        if (_namespaces.ContainsKey(tenantId))
        {
            throw new InvalidOperationException($"Broker namespace already provisioned for tenant '{tenantId}'.");
        }

        _namespaces[tenantId] = config;
        _logger.LogInformation(
            "Broker namespace provisioned for tenant {TenantId}: prefix={NamespacePrefix}, isolation={IsolationLevel}",
            tenantId, config.NamespacePrefix, config.IsolationLevel);

        return Task.FromResult(config);
    }

    /// <inheritdoc />
    public Task<bool> DeprovisionNamespaceAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var removed = _namespaces.TryRemove(tenantId, out _);
        if (removed)
        {
            _logger.LogInformation("Broker namespace deprovisioned for tenant {TenantId}", tenantId);
        }
        else
        {
            _logger.LogWarning("No broker namespace found to deprovision for tenant {TenantId}", tenantId);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<BrokerNamespaceConfig?> GetNamespaceAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _namespaces.TryGetValue(tenantId, out var config);
        return Task.FromResult(config);
    }

    /// <summary>
    /// Builds a default <see cref="BrokerNamespaceConfig"/> for a tenant, using
    /// deterministic naming conventions derived from the tenant identifier and plan tier.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="plan">Subscription plan (determines isolation level).</param>
    /// <returns>A new namespace configuration.</returns>
    public static BrokerNamespaceConfig BuildDefaultConfig(string tenantId, TenantPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var sanitised = tenantId.ToLowerInvariant().Replace(' ', '-');
        var isolation = plan >= TenantPlan.Enterprise ? IsolationLevel.Dedicated : IsolationLevel.Shared;

        return new BrokerNamespaceConfig(
            TenantId: tenantId,
            NamespacePrefix: $"ns-{sanitised}",
            QueuePrefix: $"q-{sanitised}",
            TopicPrefix: $"t-{sanitised}",
            IsolationLevel: isolation);
    }
}
