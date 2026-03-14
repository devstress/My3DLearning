# Connector Architecture

## Overview

Connectors are the outbound delivery components of the Enterprise Integration Platform. Each connector encapsulates the protocol-specific logic required to communicate with an external system, including authentication, serialization, error handling, and delivery confirmation.

## Connector Interface

All connectors implement the `IConnector` interface:

```csharp
public interface IConnector
{
    string ConnectorId { get; }
    ConnectorType Type { get; }

    Task<DeliveryResult> DeliverAsync(
        IntegrationEnvelope envelope,
        ConnectorConfiguration config,
        CancellationToken cancellationToken);

    Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken);
}
```

## Connector Types

### HTTP Connector

Delivers messages to RESTful APIs and web services.

**Capabilities:**
- HTTP methods: GET, POST, PUT, PATCH, DELETE
- Content types: JSON, XML, form-encoded, binary
- Authentication: Basic, Bearer, OAuth 2.0, API Key, Client Certificate
- Request customization: custom headers, query parameters, URL templates
- Response handling: status code validation, response body capture

**Configuration:**

```json
{
  "connectorType": "HTTP",
  "baseUrl": "https://api.target-system.com/v2",
  "method": "POST",
  "path": "/orders",
  "contentType": "application/json",
  "authentication": {
    "type": "OAuth2",
    "tokenEndpoint": "https://auth.target-system.com/token",
    "clientId": "{{secret:target-client-id}}",
    "clientSecret": "{{secret:target-client-secret}}",
    "scope": "orders.write"
  },
  "headers": {
    "X-Correlation-Id": "{{envelope.correlationId}}"
  },
  "timeoutSeconds": 30,
  "retryPolicy": {
    "maxAttempts": 3,
    "initialIntervalMs": 1000,
    "backoffCoefficient": 2.0
  }
}
```

**Error Handling:**
- `2xx` responses: Delivery successful; capture response body for audit.
- `4xx` responses: Permanent failure; route to DLQ (except 429, which is retried).
- `5xx` responses: Transient failure; retry per retry policy.
- Timeout: Transient failure; retry with exponential backoff.
- Connection refused: Transient failure; circuit breaker may open.

### SFTP Connector

Delivers files to remote SFTP servers.

**Capabilities:**
- File upload with configurable remote path and naming pattern
- SSH key and password authentication
- Temporary file upload with atomic rename (prevents partial reads)
- Directory creation if not exists
- Post-upload verification (file size check)

**Configuration:**

```json
{
  "connectorType": "SFTP",
  "host": "sftp.partner.com",
  "port": 22,
  "username": "{{secret:sftp-username}}",
  "authentication": {
    "type": "SSHKey",
    "privateKey": "{{secret:sftp-private-key}}"
  },
  "remotePath": "/inbound/orders/",
  "fileNamePattern": "order_{envelope.envelopeId}_{timestamp}.json",
  "useTempFile": true,
  "createDirectory": true,
  "timeoutSeconds": 300
}
```

**Error Handling:**
- Authentication failure: Permanent error, route to DLQ.
- Connection timeout: Transient error, retry with backoff.
- Permission denied: Permanent error, route to DLQ.
- Disk full: Transient error, retry after delay.
- Partial upload: Temporary file deleted; full retry on next attempt.

### Email Connector

Delivers messages via SMTP email.

**Capabilities:**
- SMTP and SMTPS (TLS) support
- HTML and plain text email bodies
- Template-based content generation (Liquid templates)
- File attachments from envelope payload or Cassandra claim check
- Multiple recipients (To, CC, BCC)
- Delivery status notification support

**Configuration:**

```json
{
  "connectorType": "Email",
  "smtpHost": "smtp.company.com",
  "smtpPort": 587,
  "useTls": true,
  "authentication": {
    "type": "Basic",
    "username": "{{secret:smtp-username}}",
    "password": "{{secret:smtp-password}}"
  },
  "from": "integrations@company.com",
  "to": ["recipient@partner.com"],
  "subject": "Order Confirmation - {{envelope.headers.orderId}}",
  "bodyTemplate": "templates/order-confirmation.liquid",
  "attachPayload": true,
  "attachmentFileName": "order.json"
}
```

### File Connector

Writes messages to local or network file systems.

**Capabilities:**
- Write to local directories, NFS mounts, or SMB shares
- Configurable file naming patterns with timestamp and envelope metadata
- Atomic write using temporary file and rename
- Directory creation if not exists
- File encoding configuration (UTF-8, ASCII, ISO-8859-1)

**Configuration:**

```json
{
  "connectorType": "File",
  "outputPath": "/data/outbound/orders/",
  "fileNamePattern": "{envelope.messageType}_{timestamp:yyyyMMdd_HHmmss}_{envelope.envelopeId}.json",
  "encoding": "UTF-8",
  "useTempFile": true,
  "createDirectory": true,
  "overwriteExisting": false
}
```

## Retry Policies

Each connector has a configurable retry policy:

| Parameter            | HTTP Default | SFTP Default | Email Default | File Default |
|----------------------|-------------|-------------|---------------|-------------|
| Max attempts         | 3           | 3           | 3             | 3           |
| Initial interval     | 1s          | 5s          | 5s            | 1s          |
| Backoff coefficient  | 2.0         | 2.0         | 2.0           | 2.0         |
| Max interval         | 30s         | 60s         | 60s           | 10s         |

## Monitoring

### Connector Metrics

Each connector emits the following OpenTelemetry metrics:

| Metric                           | Type      | Labels                          |
|----------------------------------|-----------|---------------------------------|
| `eip.connector.requests`        | Counter   | connector_type, status, tenant  |
| `eip.connector.duration_ms`     | Histogram | connector_type, tenant          |
| `eip.connector.errors`          | Counter   | connector_type, error_type      |
| `eip.connector.circuit_breaker` | Gauge     | connector_id, state             |
| `eip.connector.retries`         | Counter   | connector_type, attempt_number  |

### Health Checks

Each connector instance provides a health check:

- **Healthy** — Recent deliveries succeeding; circuit breaker closed.
- **Degraded** — Elevated error rate; circuit breaker half-open.
- **Unhealthy** — All deliveries failing; circuit breaker open.

### Connector Tracing

Every delivery attempt generates an OpenTelemetry span:

```
Span: Connector.Deliver
  ├── Attributes:
  │   ├── eip.connector.id = "http-erp-orders"
  │   ├── eip.connector.type = "HTTP"
  │   ├── eip.envelope_id = "env-001"
  │   ├── http.method = "POST"
  │   ├── http.url = "https://api.erp.com/orders"
  │   └── http.status_code = 201
  └── Events:
      ├── "Request sent" (timestamp)
      └── "Response received" (timestamp, status)
```

## Adding New Connectors

To add a new connector type:

1. Create a class implementing `IConnector`.
2. Define a configuration model for the connector's settings.
3. Implement delivery logic with OpenTelemetry instrumentation.
4. Implement the health check endpoint.
5. Register the connector in the dependency injection container.
6. Add unit tests for delivery, error handling, and retry behavior.
7. Add integration tests with a mock target system.
8. Document the connector configuration in this guide.

The AI code generation system can scaffold new connectors from natural language descriptions, producing the implementation, configuration model, tests, and documentation.
