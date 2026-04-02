namespace EnterpriseIntegrationPlatform.MultiTenancy.Onboarding;

/// <summary>
/// Configuration for a tenant's isolated broker namespace, including naming
/// conventions for queues and topics.
/// </summary>
/// <param name="TenantId">The tenant that owns this namespace.</param>
/// <param name="NamespacePrefix">Prefix applied to all broker resources for the tenant.</param>
/// <param name="QueuePrefix">Prefix for queue names within the namespace.</param>
/// <param name="TopicPrefix">Prefix for topic names within the namespace.</param>
/// <param name="IsolationLevel">Whether the namespace is shared or dedicated.</param>
public sealed record BrokerNamespaceConfig(
    string TenantId,
    string NamespacePrefix,
    string QueuePrefix,
    string TopicPrefix,
    IsolationLevel IsolationLevel);
