# Tutorial 34 — HTTP Connector

## What You'll Learn

- The EIP Channel Adapter pattern for connecting to external HTTP services
- How `IHttpConnector` sends integration messages over HTTP with authentication
- OAuth 2.0, Bearer token, API Key, and Client Certificate authentication modes
- Token caching to avoid redundant token requests
- `ConnectorResult` with `Success` and `ErrorMessage` that drive Ack/Nack decisions

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

The HTTP connector is the **last mile** of outbound delivery. It takes an `IntegrationEnvelope`, constructs an HTTP request with appropriate authentication, sends it, and returns a `ConnectorResult` indicating success or failure.

---

## Platform Implementation

### IHttpConnector

```csharp
// src/Connector.Http/IHttpConnector.cs
public interface IHttpConnector
{
    Task<ConnectorResult> SendAsync(
        IntegrationEnvelope<string> envelope,
        HttpConnectorOptions options,
        CancellationToken cancellationToken = default);
}
```

### HttpConnectorOptions

```csharp
// src/Connector.Http/HttpConnectorOptions.cs
public sealed class HttpConnectorOptions
{
    public required Uri Endpoint { get; init; }
    public HttpMethod Method { get; init; } = HttpMethod.Post;
    public required AuthenticationMode AuthMode { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public IDictionary<string, string>? CustomHeaders { get; init; }

    // OAuth 2.0
    public Uri? TokenEndpoint { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Scope { get; init; }

    // Bearer
    public string? BearerToken { get; init; }

    // API Key
    public string? ApiKey { get; init; }
    public string ApiKeyHeader { get; init; } = "X-Api-Key";

    // Client Certificate
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
}

public enum AuthenticationMode
{
    None,
    OAuth2ClientCredentials,
    BearerToken,
    ApiKey,
    ClientCertificate
}
```

| Auth Mode | Credentials | Use Case |
|-----------|-------------|----------|
| `OAuth2ClientCredentials` | ClientId + ClientSecret → token endpoint | Modern APIs with token exchange |
| `BearerToken` | Static or pre-obtained token | Simple integrations, pre-shared tokens |
| `ApiKey` | API key in configurable header | Legacy APIs, webhook receivers |
| `ClientCertificate` | X.509 certificate file | Mutual TLS, banking, government |

### Token Caching

For OAuth 2.0, the connector caches tokens until they expire (minus a safety margin). This avoids a token request for every message:

```csharp
// Token cache key: "{tokenEndpoint}|{clientId}|{scope}"
// Cache entry: { AccessToken, ExpiresAt }
// Refresh when: ExpiresAt - 60 seconds < now
```

### ConnectorResult

```csharp
// src/Connector.Http/ConnectorResult.cs
public sealed record ConnectorResult
{
    public bool Success { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}
```

The pipeline uses `ConnectorResult` to decide:
- **Success** → Ack the source message
- **Transient failure** (5xx, timeout) → Nack for retry (Tutorial 24)
- **Permanent failure** (4xx) → Dead-letter (Tutorial 25)

---

## Scalability Dimension

The HTTP connector is **stateless** — each request is independent. `HttpClient` instances are pooled via `IHttpClientFactory` to avoid socket exhaustion. Multiple consumer replicas can send to the same external API concurrently. The `Timeout` option and retry framework (Tutorial 24) together ensure that slow external APIs do not block the entire pipeline.

---

## Atomicity Dimension

The source message is **Acked only after a successful `ConnectorResult`**. If the HTTP request fails and retries are exhausted, the message is dead-lettered with the full `ErrorMessage` and `HttpStatusCode`. The connector never partially delivers — it sends the complete payload or fails entirely. Token cache failures are transient and trigger a fresh token request on the next attempt.

---

## Exercises

1. An external API requires OAuth 2.0 with scope `"api.write"`. Write the `HttpConnectorOptions` configuration.

2. The external API returns HTTP 503. Trace the flow through `ConnectorResult`, the retry framework, and the DLQ.

3. Why does the connector cache tokens with a 60-second safety margin before expiry rather than using them until the exact expiration time?

---

**Previous: [← Tutorial 33 — Security](33-security.md)** | **Next: [Tutorial 35 — SFTP Connector →](35-connector-sftp.md)**
