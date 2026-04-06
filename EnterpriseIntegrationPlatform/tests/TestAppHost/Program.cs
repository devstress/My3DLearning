// ============================================================================
// TestAppHost – Lightweight Aspire AppHost for tutorial integration tests.
// ============================================================================
// Mirrors the production AppHost but starts only the infrastructure containers
// that tutorials actually need: NATS JetStream, Temporal, SFTP, and SMTP.
// No Ollama/RagFlow/Cassandra/Grafana = fast startup for test suites.
// ============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// ── NATS JetStream — message broker for all tutorials ────────────────────────
// Used by 48 of 50 tutorials for real publish/subscribe message delivery.
var nats = builder.AddContainer("nats", "nats", "latest")
    .WithArgs("--jetstream")
    .WithEndpoint(targetPort: 4222, name: "nats-client", scheme: "nats");

// ── Temporal — workflow orchestration for T07, T14, T46 ──────────────────────
var temporal = builder.AddContainer("temporal", "temporalio/auto-setup", "1.29.4")
    .WithEndpoint(targetPort: 7233, name: "temporal-grpc", scheme: "http");

// ── SFTP Server — file transfer for T35 ──────────────────────────────────────
// atmoz/sftp provides a lightweight OpenSSH-based SFTP server.
var sftp = builder.AddContainer("sftp", "atmoz/sftp", "latest")
    .WithArgs("testuser:testpass:1001")
    .WithEndpoint(targetPort: 22, name: "sftp-ssh", scheme: "tcp");

// ── MailHog — SMTP capture server for T36 ────────────────────────────────────
// Captures all emails and exposes a REST API for verification.
var mailhog = builder.AddContainer("mailhog", "mailhog/mailhog", "latest")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp")
    .WithHttpEndpoint(targetPort: 8025, name: "mailhog-api");

builder.Build().Run();
