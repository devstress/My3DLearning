# Tutorial 34 — HTTP Connector

## What You'll Learn

- The EIP Channel Adapter pattern for connecting to external HTTP services
- How `IHttpConnector` provides generic `SendAsync` and `SendWithTokenAsync` methods
- Token caching via `SendWithTokenAsync` to avoid redundant token requests
- `ConnectorResult` with `Success`, `ConnectorName`, and `ErrorMessage` that drive Ack/Nack decisions

---

## EIP Pattern: Channel Adapter (HTTP)

> *"A Channel Adapter connects an application to the messaging system so that it can send and receive messages. An outbound HTTP adapter translates an integration message into an HTTP request."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  HTTP Connector   │────▶│  External    │
  │  (envelope)  │     │  (auth + send)    │     │  HTTP API    │
  └──────────────┘     └──────────────────┘     └──────────────┘
                              │
                       ┌──────┴──────┐
                       │ Token Cache │
                       └─────────────┘
```

The HTTP connector is the **last mile** of outbound delivery. It takes an `IntegrationEnvelope<TPayload>`, constructs an HTTP request (optionally with token-based authentication), sends it, and deserializes the response into a `TResponse`.

---

## Platform Implementation

### IHttpConnector

```csharp
// src/Connector.Http/IHttpConnector.cs
public interface IHttpConnector
{
    Task<TResponse> SendAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        CancellationToken ct);

    Task<TResponse> SendWithTokenAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string tokenEndpoint,
        string tokenRequestBody,
        string tokenHeaderName,
        CancellationToken ct);
}
```

### HttpConnectorOptions

```csharp
// src/Connector.Http/HttpConnectorOptions.cs
public sealed class HttpConnectorOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int CacheTokenExpirySeconds { get; set; } = 300;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}
```

| Option | Purpose |
|--------|---------|
| `BaseUrl` | Root URL of the target HTTP service; relative URLs are appended to this |
| `TimeoutSeconds` | HTTP request timeout (default 30) |
| `MaxRetryAttempts` | Max retries on transient failure (default 3) |
| `RetryDelayMs` | Base delay between retries in ms (default 1000) |
| `CacheTokenExpirySeconds` | Seconds before cached auth tokens expire (default 300) |
| `DefaultHeaders` | Headers added to every outgoing request |

### Token Caching

When using `SendWithTokenAsync`, the connector obtains a bearer token from the configured `tokenEndpoint` and caches it for the duration specified by `CacheTokenExpirySeconds` (default 300 seconds). Subsequent calls reuse the cached token until it expires:

```csharp
// SendWithTokenAsync fetches and caches the token automatically.
// tokenHeaderName = "Authorization" → sends "Bearer <token>"
// tokenHeaderName = anything else  → sends the raw token as the header value
```

### ConnectorResult

```csharp
// src/Connectors/ConnectorResult.cs
public sealed record ConnectorResult
{
    public required bool Success { get; init; }
    public required string ConnectorName { get; init; }
    public string? StatusMessage { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    public static ConnectorResult Ok(string connectorName, string? statusMessage = null) =>
        new() { Success = true, ConnectorName = connectorName, StatusMessage = statusMessage };

    public static ConnectorResult Fail(string connectorName, string errorMessage) =>
        new() { Success = false, ConnectorName = connectorName, ErrorMessage = errorMessage };
}
```

The pipeline uses `ConnectorResult` to decide:
- **Success** → Ack the source message
- **Transient failure** (timeout, network error) → Nack for retry (Tutorial 24)
- **Permanent failure** (bad request, auth failure) → Dead-letter (Tutorial 25)

---

## Scalability Dimension

The HTTP connector is **stateless** — each request is independent. `HttpClient` instances are pooled via `IHttpClientFactory` to avoid socket exhaustion. Multiple consumer replicas can send to the same external API concurrently. The `TimeoutSeconds` option and retry framework (Tutorial 24) together ensure that slow external APIs do not block the entire pipeline.

---

## Atomicity Dimension

The source message is **Acked only after a successful `ConnectorResult`**. If the HTTP request fails and retries are exhausted, the message is dead-lettered with the full `ErrorMessage`. The connector never partially delivers — it sends the complete payload or fails entirely. Token cache failures are transient and trigger a fresh token request on the next attempt.

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial34/Lab.cs`](../tests/TutorialLabs/Tutorial34/Lab.cs)

**Objective:** Trace the HTTP connector's token-based authentication, analyze retry behavior for **atomic** delivery to external APIs, and evaluate token caching for **scalable** high-volume integration.

### Step 1: Configure Token-Based Authentication

An external API requires a token from `https://auth.example.com/token`. Write the connector configuration:

```csharp
await connector.SendWithTokenAsync<string, object>(
    envelope,
    relativeUrl: "/orders",
    method: HttpMethod.Post,
    tokenEndpoint: "https://auth.example.com/token",
    tokenRequestBody: "grant_type=client_credentials&client_id=eip-platform&client_secret=" +
        await secretProvider.GetSecretAsync("partner-api-secret"),
    tokenHeaderName: "Authorization",
    ct: ct);
```

Open `src/Connector.Http/HttpConnector.cs` and trace: How does the connector obtain, cache, and refresh tokens?

### Step 2: Trace Retry and DLQ for External API Failures

The external API returns HTTP 503 (Service Unavailable). Trace the flow:

1. First attempt → 503 → retry with exponential backoff
2. Retry 1 → 503 → retry again
3. After `MaxRetryAttempts` exhausted → where does the message go?
4. What `DeadLetterReason` is set?

Now: what if the API returns HTTP 400 (Bad Request)? Is this retryable? How does the connector distinguish transient vs. permanent failures for **atomic** delivery guarantees?

### Step 3: Evaluate Token Caching for Scalability

At 5,000 messages/second, each requiring authentication:

| Strategy | Token Requests/sec | Latency Impact |
|----------|-------------------|----------------|
| Token per request | 5,000 | +200ms per message (auth round-trip) |
| Cached with `CacheTokenExpirySeconds = 300` | ~0.003 (1 per 5 min) | ~0ms (memory read) |

Why is token caching essential for **throughput scalability**? What risk does stale token caching introduce? How does `CacheTokenExpirySeconds` balance performance and security?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial34/Exam.cs`](../tests/TutorialLabs/Tutorial34/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 33 — Security](33-security.md)** | **Next: [Tutorial 35 — SFTP Connector →](35-connector-sftp.md)**
