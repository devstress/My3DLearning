namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Identifies a throttle partition — the granular unit to which a throttle
/// policy applies. Mirrors BizTalk host-throttling and Camel per-route
/// throttle dimensions: tenant, queue/topic, and endpoint/server.
/// </summary>
public sealed record ThrottlePartitionKey
{
    /// <summary>
    /// Tenant/customer identifier. When set, throttle is scoped to this tenant.
    /// Equivalent to BizTalk "Host Instance" or Camel route-level throttle per tenant.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Queue or topic name. When set, throttle is scoped to this queue.
    /// Equivalent to BizTalk "Receive Location" or Camel "from()" endpoint throttle.
    /// </summary>
    public string? Queue { get; init; }

    /// <summary>
    /// Endpoint or server identifier. When set, throttle is scoped to this endpoint.
    /// Equivalent to BizTalk "Send Port" or Camel "to()" endpoint throttle.
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Returns a normalized string key for dictionary lookups.
    /// Format: <c>tenant:{TenantId}|queue:{Queue}|endpoint:{Endpoint}</c>.
    /// </summary>
    public string ToKey() =>
        $"tenant:{TenantId ?? "*"}|queue:{Queue ?? "*"}|endpoint:{Endpoint ?? "*"}";

    /// <summary>
    /// The global partition — applies when no specific partition matches.
    /// </summary>
    public static readonly ThrottlePartitionKey Global = new();
}
