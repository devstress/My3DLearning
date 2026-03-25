namespace EnterpriseIntegrationPlatform.MultiTenancy;

/// <summary>
/// Exception thrown when a message envelope's tenant does not match the expected tenant,
/// indicating a cross-tenant data access attempt.
/// </summary>
public sealed class TenantIsolationException : Exception
{
    /// <summary>The message ID of the offending envelope.</summary>
    public Guid MessageId { get; }

    /// <summary>The tenant ID found on the envelope, or <c>null</c> if absent.</summary>
    public string? ActualTenantId { get; }

    /// <summary>The tenant ID that was expected.</summary>
    public string ExpectedTenantId { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="TenantIsolationException"/>.
    /// </summary>
    public TenantIsolationException(
        Guid messageId, string? actual, string expected, string detail)
        : base(detail)
    {
        MessageId = messageId;
        ActualTenantId = actual;
        ExpectedTenantId = expected;
    }
}
