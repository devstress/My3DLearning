namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Provisions and manages isolated broker namespaces for individual tenants.
/// </summary>
public interface IBrokerNamespaceProvisioner
{
    /// <summary>
    /// Provisions a new broker namespace with the specified configuration.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="config">Namespace configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The finalised broker namespace configuration.</returns>
    Task<BrokerNamespaceConfig> ProvisionNamespaceAsync(
        string tenantId,
        BrokerNamespaceConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprovisions a tenant's broker namespace and releases all associated resources.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the namespace was found and removed; <c>false</c> otherwise.</returns>
    Task<bool> DeprovisionNamespaceAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the broker namespace configuration for a tenant.
    /// </summary>
    /// <param name="tenantId">Unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The namespace configuration, or <c>null</c> if not provisioned.</returns>
    Task<BrokerNamespaceConfig?> GetNamespaceAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
