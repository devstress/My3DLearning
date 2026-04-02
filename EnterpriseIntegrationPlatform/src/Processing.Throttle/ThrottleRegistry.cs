using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Manages multiple <see cref="IMessageThrottle"/> instances partitioned by
/// tenant, queue, and endpoint — analogous to BizTalk host-level throttling
/// and Apache Camel per-route throttle EIP. Each partition gets its own
/// independent token bucket.
/// </summary>
/// <remarks>
/// <para>
/// Partitions are resolved in specificity order:
/// 1. Exact match (tenant + queue + endpoint)
/// 2. Tenant + queue
/// 3. Tenant only
/// 4. Queue only
/// 5. Global fallback
/// </para>
/// <para>
/// Admins can add, update, remove, and query policies at runtime via the
/// Admin API — changes take effect immediately without restart.
/// </para>
/// </remarks>
public sealed class ThrottleRegistry : IThrottleRegistry, IDisposable
{
    private readonly ConcurrentDictionary<string, ManagedThrottle> _throttles = new();
    private readonly ThrottleOptions _defaultOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ThrottleRegistry> _logger;

    /// <summary>
    /// Initializes a new <see cref="ThrottleRegistry"/> with default options
    /// and a global throttle partition.
    /// </summary>
    public ThrottleRegistry(
        IOptions<ThrottleOptions> defaultOptions,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _defaultOptions = defaultOptions.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ThrottleRegistry>();

        // Register the global partition with default options.
        var globalPolicy = new ThrottlePolicy
        {
            PolicyId = "global",
            Name = "Global Default",
            Partition = ThrottlePartitionKey.Global,
            MaxMessagesPerSecond = _defaultOptions.MaxMessagesPerSecond,
            BurstCapacity = _defaultOptions.BurstCapacity,
            MaxWaitTime = _defaultOptions.MaxWaitTime,
            RejectOnBackpressure = _defaultOptions.RejectOnBackpressure,
        };

        SetPolicy(globalPolicy);
    }

    /// <inheritdoc />
    public IMessageThrottle Resolve(ThrottlePartitionKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Try exact match first, then progressively less specific keys.
        if (TryGet(key, out var throttle))
            return throttle;

        // Tenant + queue (any endpoint)
        if (key.Endpoint is not null)
        {
            var tqKey = new ThrottlePartitionKey { TenantId = key.TenantId, Queue = key.Queue };
            if (TryGet(tqKey, out throttle))
                return throttle;
        }

        // Tenant only
        if (key.TenantId is not null)
        {
            var tKey = new ThrottlePartitionKey { TenantId = key.TenantId };
            if (TryGet(tKey, out throttle))
                return throttle;
        }

        // Queue only
        if (key.Queue is not null)
        {
            var qKey = new ThrottlePartitionKey { Queue = key.Queue };
            if (TryGet(qKey, out throttle))
                return throttle;
        }

        // Global fallback
        return _throttles[ThrottlePartitionKey.Global.ToKey()].Throttle;
    }

    /// <inheritdoc />
    public ThrottleResult ResolveAndAcquire<T>(
        IntegrationEnvelope<T> envelope,
        ThrottlePartitionKey key,
        CancellationToken ct = default)
    {
        var throttle = Resolve(key);
        return throttle.AcquireAsync(envelope, ct).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void SetPolicy(ThrottlePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var key = policy.Partition.ToKey();
        var options = Options.Create(new ThrottleOptions
        {
            MaxMessagesPerSecond = policy.MaxMessagesPerSecond,
            BurstCapacity = policy.BurstCapacity,
            MaxWaitTime = policy.MaxWaitTime,
            RejectOnBackpressure = policy.RejectOnBackpressure,
        });

        _throttles.AddOrUpdate(
            key,
            _ =>
            {
                _logger.LogInformation(
                    "Throttle policy created: {PolicyId} for partition {PartitionKey} ({Rate} msg/s, burst {Burst})",
                    policy.PolicyId, key, policy.MaxMessagesPerSecond, policy.BurstCapacity);

                return new ManagedThrottle(
                    policy,
                    new TokenBucketThrottle(options, _loggerFactory.CreateLogger<TokenBucketThrottle>()));
            },
            (_, existing) =>
            {
                _logger.LogInformation(
                    "Throttle policy updated: {PolicyId} for partition {PartitionKey} ({Rate} msg/s, burst {Burst})",
                    policy.PolicyId, key, policy.MaxMessagesPerSecond, policy.BurstCapacity);

                existing.Throttle.Dispose();
                return new ManagedThrottle(
                    policy,
                    new TokenBucketThrottle(options, _loggerFactory.CreateLogger<TokenBucketThrottle>()));
            });
    }

    /// <inheritdoc />
    public bool RemovePolicy(string policyId)
    {
        ArgumentNullException.ThrowIfNull(policyId);

        // Cannot remove the global policy.
        foreach (var (key, managed) in _throttles)
        {
            if (string.Equals(managed.Policy.PolicyId, policyId, StringComparison.Ordinal)
                && !string.Equals(policyId, "global", StringComparison.Ordinal))
            {
                if (_throttles.TryRemove(key, out var removed))
                {
                    removed.Throttle.Dispose();
                    _logger.LogInformation("Throttle policy removed: {PolicyId}", policyId);
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<ThrottlePolicyStatus> GetAllPolicies()
    {
        return _throttles.Values
            .Select(m => new ThrottlePolicyStatus
            {
                Policy = m.Policy,
                Metrics = m.Throttle.GetMetrics(),
            })
            .ToList();
    }

    /// <inheritdoc />
    public ThrottlePolicyStatus? GetPolicy(string policyId)
    {
        ArgumentNullException.ThrowIfNull(policyId);

        var managed = _throttles.Values
            .FirstOrDefault(m => string.Equals(m.Policy.PolicyId, policyId, StringComparison.Ordinal));

        if (managed is null)
            return null;

        return new ThrottlePolicyStatus
        {
            Policy = managed.Policy,
            Metrics = managed.Throttle.GetMetrics(),
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (_, managed) in _throttles)
        {
            managed.Throttle.Dispose();
        }

        _throttles.Clear();
    }

    private bool TryGet(ThrottlePartitionKey key, out IMessageThrottle throttle)
    {
        if (_throttles.TryGetValue(key.ToKey(), out var managed) && managed.Policy.IsEnabled)
        {
            throttle = managed.Throttle;
            return true;
        }

        throttle = null!;
        return false;
    }

    private sealed record ManagedThrottle(ThrottlePolicy Policy, TokenBucketThrottle Throttle);
}
