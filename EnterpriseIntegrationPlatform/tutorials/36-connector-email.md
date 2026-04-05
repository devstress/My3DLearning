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

## Lab

**Objective:** Design email delivery with throttling integration, trace the connector's notification pipeline, and analyze **atomic** delivery confirmation for email-based integrations.

### Step 1: Write a Body Builder for Order Confirmation

Write a `Func<OrderPayload, string>` body builder that creates an HTML email:

```csharp
Func<OrderPayload, string> bodyBuilder = order =>
    $"""
    <h1>Order Confirmation</h1>
    <p>Order ID: {order.OrderId}</p>
    <p>Total: ${order.Total:F2}</p>
    <p>Thank you for your purchase!</p>
    """;
```

Open `src/Connectors.Email/EmailConnector.cs` and trace: How does the connector use this builder? Why does it use `Func<T, string>` rather than a template engine?

### Step 2: Integrate Throttling for SMTP Rate Limits

An SMTP server limits sending to 100 emails per minute. Design the integration with the Throttle from Tutorial 29:

```
Message arrives → Throttle (100/min, per-tenant) → Email Connector → SMTP Send → Ack/Nack
```

What happens when the 101st email arrives within the minute? Does it queue, reject, or backpressure? How does this prevent the SMTP server from rejecting connections — a **scalability** concern for high-volume notification systems?

### Step 3: Analyze Delivery Atomicity

Email delivery is inherently non-atomic — you can't "uncommit" a sent email. Design a strategy:

| Scenario | Connector Response | Pipeline Action |
|----------|-------------------|----------------|
| SMTP accepts message | Ack | Mark as delivered |
| SMTP rejects (invalid address) | Nack | Route to DLQ |
| SMTP timeout | Retry | Exponential backoff |
| Email sent but recipient bounce | ? | How do you detect this? |

Why is email the most challenging connector for **atomicity**? How does the platform handle "fire-and-forget" delivery?

## Exam

1. Why does the email connector use `Func<T, string>` body builders rather than a template engine?
   - A) Template engines are not supported in .NET
   - B) Lambdas are compiled code — they're type-safe, refactorable, and don't require a separate template syntax; for an integration platform where emails are programmatic notifications, code-based builders are simpler and more maintainable
   - C) Templates are slower than string interpolation
   - D) The SMTP protocol requires plain strings

2. Why is throttle integration essential for email connector **scalability**?
   - A) Throttling reduces email content size
   - B) SMTP servers enforce rate limits — exceeding them causes connection rejection and delivery failure for all consumers; throttling ensures the platform respects server limits while queuing excess messages for later delivery
   - C) Email delivery doesn't benefit from throttling
   - D) Throttling is only needed for premium tenants

3. What makes email delivery uniquely challenging for **processing atomicity**?
   - A) Email is always delivered successfully
   - B) Email delivery is one-way and non-reversible — once the SMTP server accepts the message, it cannot be recalled; the platform can only confirm SMTP acceptance, not final delivery to the recipient's inbox
   - C) SMTP supports two-phase commit
   - D) Email is synchronous and always returns a delivery receipt

---

**Previous: [← Tutorial 35 — SFTP Connector](35-connector-sftp.md)** | **Next: [Tutorial 37 — File Connector →](37-connector-file.md)**
