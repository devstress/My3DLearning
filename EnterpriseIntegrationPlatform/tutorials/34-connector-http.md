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

## Exercises

1. An external API requires a token obtained from `https://auth.example.com/token`. Write a `SendWithTokenAsync` call with appropriate parameters.

2. The external API returns HTTP 503. Trace the flow through the retry framework (`MaxRetryAttempts` / `RetryDelayMs`) and the DLQ.

3. Why does the connector cache tokens with `CacheTokenExpirySeconds` rather than fetching a new token for every request?

---

**Previous: [← Tutorial 33 — Security](33-security.md)** | **Next: [Tutorial 35 — SFTP Connector →](35-connector-sftp.md)**
