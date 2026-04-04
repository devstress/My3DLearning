# Tutorial 36 — Email Connector

## What You'll Learn

- The EIP Channel Adapter pattern applied to email delivery
- How `IEmailConnector` sends integration messages as email notifications
- SMTP and SMTPS (TLS) transport configuration
- Liquid templates for dynamic subject lines and email bodies
- Attachment support from envelope payload or external references
- Single and multiple recipient routing

---

## EIP Pattern: Channel Adapter (Email)

> *"An Email Channel Adapter translates integration messages into email notifications, delivering them via SMTP to one or more recipients."*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  Email Connector  │────▶│  SMTP Server │
  │  (envelope)  │     │  (template +     │     │              │
  └──────────────┘     │   send)          │     └──────┬───────┘
                       └──────────────────┘            │
                              │                        ▼
                       ┌──────┴──────┐          ┌─────────────┐
                       │ Liquid      │          │ Recipients  │
                       │ Templates   │          │ (inbox)     │
                       └─────────────┘          └─────────────┘
```

Email is a common notification channel in enterprise integration. The connector bridges the messaging pipeline and email delivery, using templates to generate human-readable messages from structured data.

---

## Platform Implementation

### IEmailConnector

```csharp
// src/Connector.Email/IEmailConnector.cs
public interface IEmailConnector
{
    Task<ConnectorResult> SendAsync(
        IntegrationEnvelope<string> envelope,
        EmailConnectorOptions options,
        CancellationToken cancellationToken = default);
}
```

### EmailConnectorOptions

```csharp
// src/Connector.Email/EmailConnectorOptions.cs
public sealed class EmailConnectorOptions
{
    public required string SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public bool UseTls { get; init; } = true;
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string FromAddress { get; init; }
    public string? FromDisplayName { get; init; }
    public required IReadOnlyList<string> ToAddresses { get; init; }
    public IReadOnlyList<string>? CcAddresses { get; init; }
    public IReadOnlyList<string>? BccAddresses { get; init; }
    public required string SubjectTemplate { get; init; }
    public required string BodyTemplate { get; init; }
    public bool IsHtml { get; init; } = true;
    public IReadOnlyList<EmailAttachment>? Attachments { get; init; }
}
```

### Liquid Templates

Subject and body use [Liquid](https://shopify.github.io/liquid/) template syntax with the envelope as the data model:

**Subject template:**
```liquid
Order {{ envelope.CorrelationId }} — {{ envelope.MessageType }} Notification
```

**Body template:**
```liquid
<h2>Integration Notification</h2>
<p>Message ID: {{ envelope.MessageId }}</p>
<p>Source: {{ envelope.Source }}</p>
<p>Received: {{ envelope.CreatedAt | date: "%Y-%m-%d %H:%M" }}</p>
<hr/>
<pre>{{ envelope.Payload }}</pre>
```

The connector parses the envelope into a Liquid context, renders both templates, and constructs the email message.

### EmailAttachment

```csharp
// src/Connector.Email/EmailAttachment.cs
public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);
```

Attachments can be derived from the envelope payload (e.g. a PDF or CSV) or from external references resolved before sending.

### Recipient Routing

The connector supports multiple delivery strategies:

| Strategy | Configuration | Use Case |
|----------|--------------|----------|
| Single recipient | One `ToAddresses` entry | Direct notification to a specific user |
| Multiple recipients | Multiple `ToAddresses` entries | Team-wide alerts |
| CC/BCC | `CcAddresses` / `BccAddresses` | Compliance copies, audit trails |
| Dynamic | Template: `{{ envelope.Metadata.NotifyEmail }}` | Recipient determined per message |

---

## Scalability Dimension

Email sending is **I/O-bound** and relatively slow compared to broker-based delivery. The connector uses connection pooling to reuse SMTP connections across messages. For high-volume email scenarios, consider a dedicated email queue with its own consumer group to isolate email latency from the main pipeline. Rate limiting (Tutorial 29) should be applied to avoid SMTP server throttling.

---

## Atomicity Dimension

The source message is **Acked only after SMTP confirmation**. If the SMTP server rejects the email (invalid recipient, authentication failure), the connector returns a failure `ConnectorResult` and the message is retried or dead-lettered. Liquid template rendering failures are caught before sending — a malformed template results in an immediate Nack with a clear error message. Attachments are validated for size before sending.

---

## Exercises

1. Write a `SubjectTemplate` and `BodyTemplate` for an order confirmation email that includes the order ID from `CorrelationId` and the order total from the payload.

2. An SMTP server limits sending to 100 emails per minute. How would you integrate the throttle from Tutorial 29?

3. Why does the connector render templates before attempting SMTP delivery rather than during the SMTP transaction?

---

**Previous: [← Tutorial 35 — SFTP Connector](35-connector-sftp.md)** | **Next: [Tutorial 37 — File Connector →](37-connector-file.md)**
