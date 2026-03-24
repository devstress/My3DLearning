using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Enforces tenant isolation rules across message envelopes.
/// Prevents cross-tenant data access by validating that a message's tenant metadata
/// matches the expected tenant identifier.
/// </summary>
public interface ITenantIsolationGuard
{
    /// <summary>
    /// Validates that the <paramref name="envelope"/> belongs to the
    /// <paramref name="expectedTenantId"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope to validate.</param>
    /// <param name="expectedTenantId">The tenant ID the envelope must belong to.</param>
    /// <exception cref="TenantIsolationException">
    /// Thrown when the envelope's tenant does not match the expected tenant.
    /// </exception>
    void Enforce<T>(IntegrationEnvelope<T> envelope, string expectedTenantId);
}
