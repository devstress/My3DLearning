// ============================================================================
// TestAppHost – Lightweight Aspire AppHost for tutorial integration tests.
// ============================================================================
// Mirrors the production AppHost but starts only the infrastructure containers
// that tutorials actually need: NATS JetStream, Temporal, SFTP, SMTP,
// PostgreSQL, Kafka, and Pulsar. All brokers are mandatory — tests verify
// real end-to-end broker delivery.
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

// ── Apache Kafka — high-throughput event streaming for T05 and broker tests ──
// Uses official Apache Kafka image in KRaft mode (no ZooKeeper).
// IMPORTANT: isProxied: false is required because Kafka's binary protocol needs
// the advertised listener port to match the actual host port. With Aspire's TCP
// proxy, the proxy port is dynamic and cannot be injected into the container's
// KAFKA_ADVERTISED_LISTENERS before startup. Direct Docker port mapping solves
// this: host:29092 → container:9092, and Kafka advertises localhost:29092.
// NOTE: Listeners MUST use ://:PORT format (not ://0.0.0.0:PORT) because
// the Apache Kafka Docker image rejects 0.0.0.0 in advertised.listeners.
var kafka = builder.AddContainer("kafka", "apache/kafka", "3.9.0")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@localhost:9093")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://:9092,CONTROLLER://:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:29092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("CLUSTER_ID", "eip-test-cluster-001")
    .WithEndpoint(port: 29092, targetPort: 9092, name: "kafka-tcp", scheme: "tcp", isProxied: false);

// ── Apache Pulsar — Key_Shared subscription for recipient-keyed distribution ─
// Standalone mode includes broker + bookie + ZooKeeper in a single container.
var pulsar = builder.AddContainer("pulsar", "apachepulsar/pulsar", "4.0.4")
    .WithArgs("bin/pulsar", "standalone")
    .WithEndpoint(targetPort: 6650, name: "pulsar-tcp", scheme: "tcp")
    .WithEndpoint(targetPort: 8080, name: "pulsar-admin", scheme: "http");

builder.Build().Run();
