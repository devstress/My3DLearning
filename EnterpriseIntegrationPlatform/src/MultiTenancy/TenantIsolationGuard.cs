using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Production implementation of <see cref="ITenantIsolationGuard"/>.
/// Reads the <c>tenantId</c> metadata key and compares it to the expected tenant.
/// </summary>
public sealed class TenantIsolationGuard : ITenantIsolationGuard
{
    private readonly ITenantResolver _resolver;

    /// <summary>Initialises a new instance of <see cref="TenantIsolationGuard"/>.</summary>
    public TenantIsolationGuard(ITenantResolver resolver)
    {
        _resolver = resolver;
    }

    /// <inheritdoc />
    public void Enforce<T>(IntegrationEnvelope<T> envelope, string expectedTenantId)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(expectedTenantId))
            throw new ArgumentException("expectedTenantId must not be empty.", nameof(expectedTenantId));

        var resolved = _resolver.Resolve(envelope.Metadata);

        if (!resolved.IsResolved)
            throw new TenantIsolationException(
                envelope.MessageId,
                actual: null,
                expected: expectedTenantId,
                "Envelope does not carry a tenant identifier.");

        if (!string.Equals(resolved.TenantId, expectedTenantId, StringComparison.Ordinal))
            throw new TenantIsolationException(
                envelope.MessageId,
                actual: resolved.TenantId,
                expected: expectedTenantId,
                $"Tenant mismatch: envelope belongs to '{resolved.TenantId}', expected '{expectedTenantId}'.");
    }
}
