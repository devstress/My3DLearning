namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Resource quotas assigned to a tenant. Quotas are determined by the tenant's plan
/// and may be adjusted at runtime via the admin API.
/// </summary>
/// <param name="MaxMessagesPerDay">Maximum number of messages the tenant can send per day.</param>
/// <param name="MaxMessageSizeBytes">Maximum size in bytes for a single message.</param>
/// <param name="MaxQueues">Maximum number of queues the tenant may create.</param>
/// <param name="MaxConnectors">Maximum number of connectors the tenant may configure.</param>
/// <param name="MaxRetentionDays">Maximum message retention period in days.</param>
/// <param name="StorageLimitMb">Maximum storage allocation in megabytes.</param>
public sealed record TenantQuota(
    long MaxMessagesPerDay,
    long MaxMessageSizeBytes,
    int MaxQueues,
    int MaxConnectors,
    int MaxRetentionDays,
    long StorageLimitMb);
