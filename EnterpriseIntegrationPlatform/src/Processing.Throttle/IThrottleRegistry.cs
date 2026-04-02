using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Registry of partitioned throttle policies. Manages per-tenant, per-queue,
/// and per-endpoint throttles — controllable from the Admin API at runtime.
/// </summary>
public interface IThrottleRegistry
{
    /// <summary>
    /// Resolves the most specific <see cref="IMessageThrottle"/> for the given
    /// partition key. Falls back to the global throttle when no specific match.
    /// </summary>
    IMessageThrottle Resolve(ThrottlePartitionKey key);

    /// <summary>
    /// Creates or updates a throttle policy. If a policy for the same partition
    /// already exists, its token bucket is replaced with the new settings.
    /// </summary>
    void SetPolicy(ThrottlePolicy policy);

    /// <summary>
    /// Removes a throttle policy by ID. The global policy cannot be removed.
    /// </summary>
    bool RemovePolicy(string policyId);

    /// <summary>
    /// Returns all registered policies with their current runtime metrics.
    /// </summary>
    IReadOnlyList<ThrottlePolicyStatus> GetAllPolicies();

    /// <summary>
    /// Returns a single policy by ID with its current runtime metrics,
    /// or <c>null</c> if not found.
    /// </summary>
    ThrottlePolicyStatus? GetPolicy(string policyId);
}
