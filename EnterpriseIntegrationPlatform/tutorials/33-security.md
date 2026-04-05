# Tutorial 33 — Security

## What You'll Learn

- How `IInputSanitizer` strips dangerous content from message payloads
- `IPayloadSizeGuard` enforces maximum payload size limits
- Script tag removal, SQL injection pattern detection, and HTML entity sanitization
- `PayloadTooLargeException` for oversized payloads
- `JwtOptions` for configuring JWT authentication and validation
- `Security.Secrets` for integrating with Azure Key Vault and HashiCorp Vault via `ISecretProvider`

---

## EIP Pattern: Message Filter (Security)

> *"A security-focused Message Filter removes or rejects messages that contain dangerous content before they reach the processing pipeline."*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Ingress     │────▶│  Input Sanitizer  │────▶│  Payload     │
  │  (raw)       │     │  (strip scripts,  │     │  Size Guard  │
  └──────────────┘     │   SQL, HTML)      │     └──────┬───────┘
                       └──────────────────┘            │
                                                       ▼
                                              ┌──────────────────┐
                                              │  Pipeline        │
                                              │  (safe payload)  │
                                              └──────────────────┘
```

The sanitizer and size guard act as a security gateway. They run before any business logic, ensuring that only clean, appropriately-sized payloads enter the pipeline.

---

## Platform Implementation

### IInputSanitizer

```csharp
// src/Security/IInputSanitizer.cs
public interface IInputSanitizer
{
    string Sanitize(string input);
    bool IsClean(string input);
}
```

`Sanitize` returns a cleaned copy of the input with dangerous content removed. `IsClean` returns `true` if the input contains no dangerous patterns (i.e., `Sanitize` would not modify it). Both methods are synchronous.

The sanitizer detects and removes:
- **Script tags**: `<script>...</script>`, inline event handlers (`onclick`, `onerror`)
- **SQL injection patterns**: `'; DROP TABLE`, `OR 1=1`, `UNION SELECT`, comment sequences
- **HTML entities**: encoded characters used to bypass text-based filters (`&#60;`, `&lt;`)
- **Control characters**: null bytes, Unicode direction overrides

### IPayloadSizeGuard

```csharp
// src/Security/IPayloadSizeGuard.cs
public interface IPayloadSizeGuard
{
    void Enforce(string payload);
    void Enforce(byte[] payloadBytes);
}
```

### PayloadTooLargeException

```csharp
// src/Security/PayloadTooLargeException.cs
public sealed class PayloadTooLargeException : Exception
{
    public int ActualBytes { get; }
    public int MaxBytes { get; }
}
```

When the payload exceeds the configured limit, the guard throws `PayloadTooLargeException`. This is a non-retryable error — the message is dead-lettered with `DeadLetterReason.ValidationFailed`.

### JwtOptions

```csharp
// src/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public bool ValidateLifetime { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
```

JWT authentication is used at the Gateway API layer. Tokens carry tenant identity (Tutorial 32) and scopes. The `JwtOptions` configure validation parameters and are bound from the `"Jwt"` configuration section via `SectionName`. The `ClockSkew` property controls how much clock drift is tolerated when validating token expiration.

### Security.Secrets

```csharp
// src/Security.Secrets/ISecretProvider.cs
public interface ISecretProvider
{
    Task<SecretEntry?> GetSecretAsync(
        string key, string? version = null, CancellationToken ct = default);
    Task<SecretEntry> SetSecretAsync(
        string key, string value,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default);
    Task<bool> DeleteSecretAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListSecretKeysAsync(
        string? prefix = null, CancellationToken ct = default);
}

public sealed record SecretEntry(
    string Key,
    string Value,
    string Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
}
```

`GetSecretAsync` returns a `SecretEntry?` containing the value along with version, creation timestamp, and metadata — or `null` if the key does not exist. The optional `version` parameter allows retrieving a specific version of a secret. `SetSecretAsync` returns the newly created `SecretEntry` (with version and timestamp) and accepts optional metadata. `DeleteSecretAsync` returns `true` if the key was deleted, `false` if it did not exist. `ListSecretKeysAsync` returns all known key names, optionally filtered by prefix.

Two implementations are provided:
- `AzureKeyVaultSecretProvider` — integrates with Azure Key Vault using managed identity
- `VaultSecretProvider` — integrates with HashiCorp Vault using AppRole or token auth

Secrets are never stored in configuration files or environment variables in production. The `ISecretProvider` abstraction allows swapping providers without code changes.

---

## Scalability Dimension

The sanitizer and size guard are **stateless and CPU-bound** — they inspect each payload independently. They run as early pipeline stages, rejecting bad messages before expensive processing occurs. This protects downstream systems from wasted work. In high-throughput deployments, sanitization can be the bottleneck; profiling guides replica count.

---

## Atomicity Dimension

Sanitization runs **before the message is Acked**. Callers can use `IsClean` to check whether the input would be modified, and `Sanitize` to obtain the cleaned payload. If the size guard rejects a message, it is Nacked and dead-lettered in a single atomic operation — the original message is never lost, just quarantined for inspection.

---

## Lab

**Objective:** Trace the input sanitization pipeline, analyze how defense-in-depth protects **message atomicity** from injection attacks, and evaluate secret management for **scalable** multi-environment deployments.

### Step 1: Trace XSS Sanitization

A payload contains `<script>alert('xss')</script>` embedded in a JSON string value. Open `src/Security/InputSanitizer.cs` and trace:

1. How does the sanitizer detect the `<script>` tag within a JSON value?
2. Is the malicious content removed, escaped, or rejected?
3. Is the rest of the valid JSON preserved?
4. What happens to the envelope's `MessageId` and `CorrelationId` — are they affected?

Why is sanitization critical for **pipeline atomicity**? (hint: unsanitized payloads could execute scripts in downstream web UIs or corrupt database queries)

### Step 2: Analyze Payload Size Guard

Why does the platform use a separate `IPayloadSizeGuard` instead of checking size inside `IInputSanitizer`?

| Concern | Responsibility | Why Separate? |
|---------|---------------|---------------|
| Size validation | `IPayloadSizeGuard` | Cheap check — reject oversized payloads before expensive sanitization |
| Content sanitization | `IInputSanitizer` | Complex parsing — only runs on right-sized payloads |
| Schema validation | Activity pipeline | Business logic — runs after sanitization |

How does this layered approach improve **throughput scalability**? (hint: fast rejection of invalid messages at each layer)

### Step 3: Compare Secret Providers for Multi-Environment Deployment

| Provider | Best For | Rotation | Caching |
|----------|---------|----------|---------|
| `InMemorySecretProvider` | Development and testing | Manual | No |
| `CachedSecretProvider` | Production (wraps any provider) | Automatic TTL | Yes |
| `AzureKeyVaultSecretProvider` | Azure deployments | Azure-managed | Via wrapper |
| `VaultSecretProvider` | Multi-cloud with HashiCorp Vault | Vault-managed | Via wrapper |

When would you use `CachedSecretProvider` wrapping `AzureKeyVaultSecretProvider`? What is the **scalability** benefit of caching secrets locally?

## Exam

1. Why does the platform sanitize payloads **before** routing or processing?
   - A) Sanitization improves message routing speed
   - B) Unsanitized payloads could contain injection attacks (XSS, SQL injection) that execute when consumed by downstream systems — sanitizing at ingress prevents malicious content from propagating through the entire pipeline
   - C) The broker requires sanitized payloads
   - D) Sanitization is only needed for XML messages

2. Why does the `IPayloadSizeGuard` run before `IInputSanitizer`?
   - A) Size checking is always done first by convention
   - B) Rejecting oversized payloads before sanitization avoids expensive parsing of potentially malicious large payloads — this is a defense-in-depth principle that protects against denial-of-service via payload size
   - C) The sanitizer cannot handle large payloads
   - D) Size checking requires less memory

3. How does secret caching with `CachedSecretProvider` improve **operational scalability**?
   - A) Caching stores more secrets than the vault
   - B) Frequently accessed secrets are served from memory instead of making network calls to the vault — this reduces latency and eliminates the vault as a bottleneck when many services need secrets simultaneously
   - C) Caching eliminates the need for secret rotation
   - D) The vault requires caching for correctness

---

**Previous: [← Tutorial 32 — Multi-Tenancy](32-multi-tenancy.md)** | **Next: [Tutorial 34 — HTTP Connector →](34-connector-http.md)**
