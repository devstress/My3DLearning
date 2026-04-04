# Tutorial 32 — Multi-Tenancy

## What You'll Learn

- How `ITenantResolver` identifies the current tenant from message metadata or HTTP headers
- `ITenantIsolationGuard` enforces cross-tenant data boundaries
- `TenantContext` carries tenant identity through the pipeline
- `TenantIsolationException` when a message crosses tenant boundaries
- Anonymous tenant handling for unauthenticated or system messages
- `MultiTenancy.Onboarding` for self-service tenant provisioning

---

## EIP Pattern: Message Metadata (Tenant Header)

> *"Attach tenant identity as message metadata so every component in the pipeline can enforce data isolation without coupling to the resolution mechanism."*

```
  ┌──────────────┐     ┌──────────────────┐
  │  Ingress     │────▶│  Tenant Resolver  │
  │  (header or  │     │  (metadata /      │
  │   metadata)  │     │   header lookup)  │
  └──────────────┘     └───────┬──────────┘
                               │
                     TenantContext flows
                     through pipeline
                               │
                    ┌──────────▼──────────┐
                    │  Isolation Guard    │
                    │  (cross-tenant      │
                    │   boundary check)   │
                    └─────────────────────┘
```

Every message entering the platform passes through tenant resolution. The resolved `TenantContext` propagates through all pipeline stages, and the isolation guard rejects any operation that would cross tenant boundaries.

---

## Platform Implementation

### ITenantResolver

```csharp
// src/MultiTenancy/ITenantResolver.cs
public interface ITenantResolver
{
    Task<TenantContext> ResolveAsync(
        IntegrationEnvelope<string> envelope,
        CancellationToken cancellationToken = default);

    Task<TenantContext> ResolveFromHeadersAsync(
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}
```

The resolver checks (in order): the `X-Tenant-Id` header, envelope metadata `TenantId` key, and the authenticated identity's tenant claim. If no tenant is found, it returns the **anonymous tenant context**.

### TenantContext

```csharp
// src/MultiTenancy/TenantContext.cs
public sealed record TenantContext
{
    public required string TenantId { get; init; }
    public required string TenantName { get; init; }
    public bool IsAnonymous { get; init; }
    public IDictionary<string, string>? Properties { get; init; }

    public static TenantContext Anonymous => new()
    {
        TenantId = "anonymous",
        TenantName = "Anonymous",
        IsAnonymous = true
    };
}
```

### ITenantIsolationGuard

```csharp
// src/MultiTenancy/ITenantIsolationGuard.cs
public interface ITenantIsolationGuard
{
    void Validate(TenantContext current, TenantContext target);
    void ValidateEnvelope(IntegrationEnvelope<string> envelope, TenantContext context);
}
```

### TenantIsolationException

```csharp
// src/MultiTenancy/TenantIsolationException.cs
public sealed class TenantIsolationException : Exception
{
    public string SourceTenantId { get; }
    public string TargetTenantId { get; }
    public string Operation { get; }
}
```

When a message from Tenant A attempts to access Tenant B's data or topics, the guard throws `TenantIsolationException`. This is **non-retryable** — the message goes directly to the DLQ.

### MultiTenancy.Onboarding

```csharp
// src/MultiTenancy.Onboarding/ITenantOnboardingService.cs
public interface ITenantOnboardingService
{
    Task<TenantContext> OnboardAsync(
        TenantOnboardingRequest request,
        CancellationToken ct);

    Task OffboardAsync(string tenantId, CancellationToken ct);
}

public sealed record TenantOnboardingRequest(
    string TenantName,
    string AdminEmail,
    IDictionary<string, string>? Properties = null);
```

Onboarding provisions: tenant-specific broker topics, throttle policies (Tutorial 29), configuration namespaces, and an admin user. Offboarding archives data and removes access.

---

## Scalability Dimension

Tenant isolation enables **horizontal scaling by tenant**. High-volume tenants can be assigned dedicated consumer groups and broker partitions while small tenants share resources. The `TenantContext.Properties` dictionary can carry tenant-specific quotas and feature flags, enabling per-tenant scaling decisions in the competing consumers orchestrator (Tutorial 28).

---

## Atomicity Dimension

The isolation guard runs **before any processing** — a cross-tenant message is rejected atomically before it can corrupt another tenant's data. The `TenantContext` is resolved once at ingress and propagated immutably through the pipeline. This ensures that every component sees the same tenant identity, preventing time-of-check/time-of-use races.

---

## Exercises

1. A message arrives with `X-Tenant-Id: tenant-a` but the JWT claim says `tenant-b`. How should the resolver handle this conflict?

2. Describe the self-service flow when a new tenant onboards: what resources are provisioned and in what order?

3. Why is `TenantIsolationException` non-retryable? Under what circumstances could a cross-tenant message be legitimate?

---

**Previous: [← Tutorial 31 — Event Sourcing](31-event-sourcing.md)** | **Next: [Tutorial 33 — Security →](33-security.md)**
