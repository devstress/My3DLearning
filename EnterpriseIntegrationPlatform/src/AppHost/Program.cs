var builder = DistributedApplication.CreateBuilder(args);

// ── AI Infrastructure ─────────────────────────────────────────────────────────
// Ollama powers the self-hosted RAG system by providing embedding and retrieval
// capabilities within RagFlow. Developers use their own AI provider (Copilot,
// Codex, Claude Code) for code generation, connecting to the RAG API for context.
// Host port 15434 avoids conflict with any existing Ollama instance on 11434.
var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithHttpEndpoint(port: 15434, targetPort: 11434, name: "ollama-api")
    .WithVolume("ollama-data", "/root/.ollama")
    .WithLifetime(ContainerLifetime.Persistent);

// RagFlow provides RAG (Retrieval-Augmented Generation) for chunking and
// querying integration framework documentation via Ollama.
// Users can ask OpenClaw to retrieve relevant context from the platform's
// indexed docs, rules, and source code. Developers then use their own
// preferred AI provider (Copilot, Codex, Claude Code) for code generation.
// Host ports 15080 (UI) and 15380 (API) avoid conflicts with common ports.
var ragflow = builder.AddContainer("ragflow", "infiniflow/ragflow", "v0.16.0-slim")
    .WithHttpEndpoint(port: 15080, targetPort: 80, name: "ragflow-ui")
    .WithHttpEndpoint(port: 15380, targetPort: 9380, name: "ragflow-api")
    .WithEnvironment("OLLAMA_BASE_URL", ollama.GetEndpoint("ollama-api"))
    .WithVolume("ragflow-data", "/ragflow/data")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Observability Infrastructure ──────────────────────────────────────────────
// Grafana Loki provides durable, queryable log storage for all message lifecycle
// events (traces, status, metadata). This replaces in-memory storage for real
// observability — event logs, traces, status, and metadata are all stored here.
// Host port 15100 avoids conflict with other Loki instances on 3100.
var loki = builder.AddContainer("loki", "grafana/loki", "3.4.2")
    .WithHttpEndpoint(port: 15100, targetPort: 3100, name: "loki-api")
    .WithVolume("loki-data", "/loki")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Workflow Orchestration ────────────────────────────────────────────────────
// Temporal provides durable, fault-tolerant workflow orchestration.
// The auto-setup image creates the default namespace on first start.
// Host port 15233 avoids conflict with any existing Temporal on 7233.
var temporal = builder.AddContainer("temporal", "temporalio/auto-setup", "1.29.4")
    .WithEndpoint(port: 15233, targetPort: 7233, name: "temporal-grpc", scheme: "http")
    .WithLifetime(ContainerLifetime.Persistent);

// Temporal Web UI for inspecting workflows, activities, and task queues.
// Host port 15280 avoids conflict with common 8080 port.
var temporalUi = builder.AddContainer("temporal-ui", "temporalio/ui", "2.47.3")
    .WithHttpEndpoint(port: 15280, targetPort: 8080, name: "temporal-ui-http")
    .WithEnvironment("TEMPORAL_ADDRESS", "temporal:7233")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Distributed Persistence ───────────────────────────────────────────────────
// Cassandra provides scalable, distributed storage for message state, faults,
// and audit data. SimpleStrategy RF=3 satisfies Quality Pillar 1 (Reliability).
// Host port 15042 avoids conflict with any existing Cassandra on 9042.
var cassandra = builder.AddContainer("cassandra", "cassandra", "5.0")
    .WithEndpoint(port: 15042, targetPort: 9042, name: "cassandra-cql", scheme: "tcp")
    .WithVolume("cassandra-data", "/var/lib/cassandra")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Platform Services ─────────────────────────────────────────────────────────

// NATS JetStream — default queue broker for task-oriented message delivery.
// Per-subject independence avoids Head-of-Line blocking between recipients.
// Host port 15222 avoids conflict with any existing NATS on 4222.
var nats = builder.AddContainer("nats", "nats", "latest")
    .WithArgs("--jetstream")
    .WithEndpoint(port: 15222, targetPort: 4222, name: "nats-client", scheme: "nats")
    .WithVolume("nats-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

// Ingestion.Kafka handles Kafka streaming workloads (audit, analytics).
// Task-oriented delivery (ingestion, routing, DLQ) uses the configurable
// queue broker — NATS JetStream (default) or Apache Pulsar (Key_Shared).
var ingestionKafka = builder.AddProject<Projects.Ingestion_Kafka>("ingestion-kafka");

var workflowTemporal = builder.AddProject<Projects.Workflow_Temporal>("workflow-temporal");

// OpenClaw – the observability + knowledge retrieval web UI – provides
// "where is my message?" trace analysis and RAG-based context retrieval
// via RagFlow. Developers connect their own AI provider (Copilot, Codex,
// Claude Code) to the RAG endpoints to get platform context for generating
// integrations.
// Loki provides real storage for all event logs, traces, status, and metadata.
var openClaw = builder.AddProject<Projects.OpenClaw_Web>("openclaw")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Ollama__BaseAddress", ollama.GetEndpoint("ollama-api"))
    .WithEnvironment("Loki__BaseAddress", loki.GetEndpoint("loki-api"))
    .WithEnvironment("RagFlow__BaseAddress", ragflow.GetEndpoint("ragflow-api"));

// Admin.Api – administration API for platform management.
// Provides authenticated, rate-limited endpoints for querying messages,
// inspecting faults, updating delivery status, and monitoring platform health.
// Protect with AdminApi:ApiKeys in configuration or Aspire secrets — never in source.
var adminApi = builder.AddProject<Projects.Admin_Api>("admin-api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Loki__BaseAddress", loki.GetEndpoint("loki-api"))
    .WithEnvironment("Cassandra__ContactPoints__0", "localhost")
    .WithEnvironment("Cassandra__Port", "15042");

// Demo.Pipeline – end-to-end integration pipeline that wires all platform
// components together: NATS JetStream inbound consumer → Temporal workflow
// (validate + log) → Cassandra persistence → NATS Ack/Nack notification.
// This demonstrates the full message lifecycle: receive → process → persist → notify.
builder.AddProject<Projects.Demo_Pipeline>("demo-pipeline")
    .WithEnvironment("Pipeline__NatsUrl", nats.GetEndpoint("nats-client"))
    .WithEnvironment("Pipeline__TemporalServerAddress", "temporal:7233")
    .WithEnvironment("Loki__BaseAddress", loki.GetEndpoint("loki-api"))
    .WithEnvironment("Cassandra__ContactPoints__0", "localhost")
    .WithEnvironment("Cassandra__Port", "15042");

// Gateway.Api – API Gateway that acts as the single entry point for all
// external integration traffic. Provides request routing, rate limiting,
// JWT authentication passthrough, and health aggregation.
// Host port 15000 for external access.
var gateway = builder.AddProject<Projects.Gateway_Api>("gateway-api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Gateway__AdminApiBaseUrl", adminApi.GetEndpoint("http"))
    .WithEnvironment("Gateway__OpenClawBaseUrl", openClaw.GetEndpoint("http"));

builder.Build().Run();
