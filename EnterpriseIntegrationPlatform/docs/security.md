# Security Architecture

## Overview

The Enterprise Integration Platform enforces defense-in-depth security across all layers. Security is a cross-cutting concern embedded in every component, from ingress API authentication to data encryption at rest in Cassandra.

## Authentication

### API Authentication

The Ingress API and Admin API use OAuth 2.0 / OpenID Connect for authentication:

- **Bearer tokens (JWT)** — Clients obtain tokens from an identity provider (e.g., Azure AD, Keycloak, Auth0).
- **Token validation** — Tokens are validated for signature, issuer, audience, and expiration on every request.
- **API keys** — Supported as an alternative for system-to-system integrations where OAuth is impractical.

### Service-to-Service Authentication

Internal communication between platform services uses mutual TLS (mTLS):

- Each service has a unique TLS certificate issued by an internal Certificate Authority.
- Certificates are rotated automatically on a configurable schedule (default: 90 days).
- Kafka, NATS/Pulsar, Temporal, and Cassandra connections all use mTLS.

### Connector Authentication

Outbound connectors support multiple authentication methods per target system:

| Method         | Use Case                                  |
|----------------|-------------------------------------------|
| None           | Public endpoints with no authentication   |
| Basic Auth     | Legacy systems requiring username/password|
| Bearer Token   | APIs using static or pre-obtained tokens  |
| OAuth 2.0      | APIs requiring dynamic token acquisition  |
| API Key        | APIs using key-based authentication       |
| Client Certificate | Systems requiring mutual TLS           |
| SSH Key        | SFTP servers using key-based auth         |

## Authorization

### Role-Based Access Control (RBAC)

The Admin API enforces RBAC with the following predefined roles:

| Role              | Permissions                                                    |
|-------------------|----------------------------------------------------------------|
| `admin`           | Full access to all resources and configuration                 |
| `operator`        | View all resources; manage DLQ; replay messages                |
| `developer`       | Manage routes, workflows, transformations; view monitoring     |
| `viewer`          | Read-only access to monitoring and configuration               |
| `tenant-admin`    | Full access scoped to a specific tenant's resources            |

### Tenant Isolation

Authorization is scoped by tenant:

- Every API request includes a tenant context derived from the authenticated identity.
- Queries to Cassandra include the tenant ID in partition keys, preventing cross-tenant data access.
- Kafka topic ACLs restrict tenant-specific topics to authorized service accounts.
- Temporal namespace separation ensures workflow isolation between tenants.

### Policy Enforcement

Authorization policies are evaluated at three levels:

1. **API Gateway** — Validates token claims and role assignments.
2. **Service Layer** — Enforces business-level authorization rules (e.g., can this user modify this route?).
3. **Data Layer** — Partition key design ensures queries cannot return cross-tenant data.

## Secret Management

### Principles

- **No secrets in code** — Secrets are never committed to source control.
- **No secrets in configuration files** — Configuration files reference secret names, not values.
- **Least privilege** — Each service has access only to the secrets it needs.

### Secret Storage

Secrets are stored in a secure vault and injected at runtime:

| Environment    | Secret Store                              |
|----------------|-------------------------------------------|
| Local Dev      | .NET User Secrets / Environment variables |
| CI/CD          | GitHub Actions Secrets                    |
| Staging/Prod   | Azure Key Vault / HashiCorp Vault / AWS Secrets Manager |

### Managed Secrets

| Secret Type             | Rotation Policy | Storage                   |
|-------------------------|-----------------|---------------------------|
| Database credentials    | 90 days         | Vault with auto-rotation  |
| Kafka SASL credentials  | 90 days         | Vault with auto-rotation  |
| NATS/Pulsar credentials | 90 days         | Vault with auto-rotation  |
| Temporal API keys       | 180 days        | Vault                     |
| Connector credentials   | Per policy      | Vault, referenced by ID   |
| TLS certificates        | 90 days         | Cert manager (auto-renew) |
| OAuth client secrets    | 365 days        | Vault                     |

## Input Validation

### Ingress Validation

All input is validated at the ingress boundary:

- **Content-Type verification** — Only accepted MIME types are processed (configurable per route).
- **Payload size limits** — Maximum payload size enforced (default: 10 MB; configurable per tenant).
- **Schema validation** — Payloads can be validated against JSON Schema or XSD definitions.
- **Header sanitization** — Message headers are sanitized to prevent injection attacks.
- **Rate limiting** — Per-tenant rate limits prevent abuse (429 Too Many Requests on excess).

### Transformation Validation

Transformation activities validate both input and output:

- Input is validated against the source schema before transformation.
- Output is validated against the target schema after transformation.
- Transformation failures are treated as non-retryable errors.

## Encryption

### Encryption in Transit

All network communication is encrypted:

| Connection             | Protocol   | Minimum TLS Version |
|------------------------|------------|---------------------|
| Client → Ingress API   | HTTPS      | TLS 1.2             |
| Client → Admin API     | HTTPS      | TLS 1.2             |
| Service → Kafka        | TLS        | TLS 1.2             |
| Service → NATS/Pulsar  | TLS        | TLS 1.2             |
| Service → Temporal     | gRPC + TLS | TLS 1.2             |
| Service → Cassandra    | CQL + TLS  | TLS 1.2             |
| Connector → Target     | HTTPS/SFTP | TLS 1.2             |

### Encryption at Rest

Data at rest is encrypted in all storage layers:

- **Cassandra** — Transparent data encryption (TDE) enabled for all tables.
- **Kafka** — Disk-level encryption enabled on broker storage volumes.
- **NATS/Pulsar** — Pulsar uses BookKeeper encryption for persistent messages; NATS JetStream uses file-level encryption on stream storage.
- **Temporal** — Payload encryption using Temporal's Data Converter with AES-256-GCM.
- **Sensitive fields** — Individual fields marked as sensitive are encrypted at the application level before storage.

## Audit Logging

### Audit Events

All security-relevant actions are recorded in an immutable audit log:

| Event Category         | Examples                                              |
|------------------------|-------------------------------------------------------|
| Authentication         | Login success/failure, token refresh, API key usage   |
| Authorization          | Access granted/denied, role changes                   |
| Configuration Changes  | Route created/modified/deleted, connector configured  |
| Data Access            | Message viewed, DLQ message replayed                  |
| System Events          | Service start/stop, health status changes             |

### Audit Log Schema

```json
{
  "eventId": "guid",
  "timestamp": "2025-01-15T10:30:45.123Z",
  "eventType": "route.created",
  "actor": {
    "userId": "user@example.com",
    "role": "developer",
    "tenantId": "tenant-acme",
    "ipAddress": "10.0.1.50"
  },
  "resource": {
    "type": "Route",
    "id": "route-001",
    "tenantId": "tenant-acme"
  },
  "details": {
    "action": "create",
    "changes": { "name": "Order Routing", "priority": 1 }
  },
  "correlationId": "corr-001"
}
```

### Audit Log Retention

- Default retention: 1 year
- Compliance-sensitive tenants: 7 years
- Audit logs are write-once and cannot be modified or deleted through the application API

## Security Testing

### Automated Security Checks

- **Dependency scanning** — GitHub Dependabot and CodeQL scan for known vulnerabilities.
- **SAST** — Static analysis integrated into CI/CD pipeline.
- **Secret scanning** — Pre-commit hooks and CI checks prevent accidental secret commits.
- **Container scanning** — Container images scanned for OS and library vulnerabilities.

### Penetration Testing

- Annual penetration testing by an external security firm.
- Quarterly internal security reviews of new features and configurations.
- Bug bounty program for responsible disclosure.
