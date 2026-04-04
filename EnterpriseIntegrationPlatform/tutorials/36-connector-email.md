# Tutorial 36 — Email Connector

## What You'll Learn

- The EIP Channel Adapter pattern applied to email delivery
- How `IEmailConnector` sends integration messages as email notifications
- SMTP and SMTPS (TLS) transport configuration
- `Func<T, string>` body builders for dynamic email content
- Single and multiple recipient overloads

---

## EIP Pattern: Channel Adapter (Email)

> *"An Email Channel Adapter translates integration messages into email notifications, delivering them via SMTP to one or more recipients."*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  Email Connector  │────▶│  SMTP Server │
  │  (envelope)  │     │  (build body +   │     │              │
  └──────────────┘     │   send)          │     └──────┬───────┘
                       └──────────────────┘            │
                                                       ▼
                                                 ┌─────────────┐
                                                 │ Recipients  │
                                                 │ (inbox)     │
                                                 └─────────────┘
```

Email is a common notification channel in enterprise integration. The connector bridges the messaging pipeline and email delivery, using `Func<T, string>` body builders to generate human-readable messages from structured data.

---

## Platform Implementation

### IEmailConnector

```csharp
// src/Connector.Email/IEmailConnector.cs
public interface IEmailConnector
{
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string toAddress,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);

    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyList<string> toAddresses,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);
}
```

Both overloads are generic over `T` and return `Task` (not `ConnectorResult`). When `subject` is `null`, the configured `DefaultSubjectTemplate` is used. The `bodyBuilder` function converts the envelope payload into the email body string.

### EmailConnectorOptions

```csharp
// src/Connector.Email/EmailConnectorOptions.cs
public sealed class EmailConnectorOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DefaultFrom { get; set; } = string.Empty;
    public string DefaultSubjectTemplate { get; set; } = "{MessageType} notification";
}
```

### Body Builders

Instead of a template engine, the connector uses `Func<T, string>` body builders that the caller provides. This gives full control over how the payload is rendered into an email body:

```csharp
// Example: build an order confirmation email body
Func<OrderPayload, string> bodyBuilder = order =>
    $"<h2>Order Confirmation</h2>" +
    $"<p>Order ID: {order.OrderId}</p>" +
    $"<p>Total: {order.Total:C}</p>";

await emailConnector.SendAsync(envelope, "ops@example.com", null, bodyBuilder, ct);
```

### Recipient Routing

The connector supports two delivery patterns via its overloads:

| Strategy | Method Overload | Use Case |
|----------|----------------|----------|
| Single recipient | `SendAsync<T>(..., string toAddress, ...)` | Direct notification to a specific user |
| Multiple recipients | `SendAsync<T>(..., IReadOnlyList<string> toAddresses, ...)` | Team-wide alerts |

---

## Scalability Dimension

Email sending is **I/O-bound** and relatively slow compared to broker-based delivery. The connector uses connection pooling to reuse SMTP connections across messages. For high-volume email scenarios, consider a dedicated email queue with its own consumer group to isolate email latency from the main pipeline. Rate limiting (Tutorial 29) should be applied to avoid SMTP server throttling.

---

## Atomicity Dimension

The source message is **Acked only after SMTP confirmation**. If the SMTP server rejects the email (invalid recipient, authentication failure), the `SendAsync` method throws and the message is retried or dead-lettered. Body builder failures are caught before sending — a failing builder results in an immediate Nack with a clear error message.

---

## Exercises

1. Write a `Func<T, string>` body builder for an order confirmation email that includes the order ID and total from the payload.

2. An SMTP server limits sending to 100 emails per minute. How would you integrate the throttle from Tutorial 29?

3. Why does the connector use `Func<T, string>` body builders rather than a template engine?

---

**Previous: [← Tutorial 35 — SFTP Connector](35-connector-sftp.md)** | **Next: [Tutorial 37 — File Connector →](37-connector-file.md)**
