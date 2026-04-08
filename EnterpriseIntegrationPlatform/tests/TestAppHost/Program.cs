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

// ── PostgreSQL — EIP Postgres message broker for integration tests ───────────
// Used by Ingestion.Postgres integration tests to verify full broker behaviour.
var postgres = builder.AddContainer("postgres", "postgres", "17")
    .WithEnvironment("POSTGRES_DB", "eip")
    .WithEnvironment("POSTGRES_USER", "eip")
    .WithEnvironment("POSTGRES_PASSWORD", "eip")
    .WithEndpoint(targetPort: 5432, name: "postgres-tcp", scheme: "tcp");

// ── Apache Kafka (via Bitnami image) — high-throughput event streaming ───────
// Uses KRaft mode (no ZooKeeper) for minimal resource footprint in tests.
var kafka = builder.AddContainer("kafka", "bitnami/kafka", "3.9.0")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "0")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "controller,broker")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "0@localhost:9093")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://:9092,CONTROLLER://:9093,EXTERNAL://:9094")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092,EXTERNAL://localhost:9094")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEndpoint(targetPort: 9094, name: "kafka-tcp", scheme: "tcp");

// ── Apache Pulsar — Key_Shared subscription for recipient-keyed distribution ─
// Standalone mode includes broker + bookie + ZooKeeper in a single container.
var pulsar = builder.AddContainer("pulsar", "apachepulsar/pulsar", "4.0.4")
    .WithArgs("bin/pulsar", "standalone")
    .WithEndpoint(targetPort: 6650, name: "pulsar-tcp", scheme: "tcp")
    .WithEndpoint(targetPort: 8080, name: "pulsar-admin", scheme: "http");

builder.Build().Run();
