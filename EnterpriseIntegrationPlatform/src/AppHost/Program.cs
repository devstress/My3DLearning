var builder = DistributedApplication.CreateBuilder(args);

// ── AI Infrastructure ─────────────────────────────────────────────────────────
// Ollama provides local LLM inference for AI-assisted observability (OpenClaw)
// and embedding/retrieval within RagFlow. Developers use their own AI provider
// (Copilot, Codex, Claude Code) for code generation.
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

// OpenClaw – the observability + context retrieval web UI – talks to Ollama for
// AI-powered diagnostics and RagFlow for RAG-based context retrieval.
// Developers connect their own AI provider (Copilot, Codex, Claude Code)
// to the RAG endpoints for integration code generation.
// Loki provides real storage for all event logs, traces, status, and metadata.
var openClaw = builder.AddProject<Projects.OpenClaw_Web>("openclaw")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Ollama__BaseAddress", ollama.GetEndpoint("ollama-api"))
    .WithEnvironment("Loki__BaseAddress", loki.GetEndpoint("loki-api"))
    .WithEnvironment("RagFlow__BaseAddress", ragflow.GetEndpoint("ragflow-api"));

builder.Build().Run();
