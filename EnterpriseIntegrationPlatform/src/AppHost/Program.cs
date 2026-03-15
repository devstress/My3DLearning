var builder = DistributedApplication.CreateBuilder(args);

// ── AI Infrastructure ─────────────────────────────────────────────────────────
// Ollama provides local LLM inference for AI-assisted observability (OpenClaw)
// and RAG-based document retrieval (RagFlow).
var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithHttpEndpoint(targetPort: 11434, name: "ollama-api")
    .WithVolume("ollama-data", "/root/.ollama")
    .WithLifetime(ContainerLifetime.Persistent);

// RagFlow provides RAG (Retrieval-Augmented Generation) for chunking and
// querying integration framework documentation via Ollama.
// Users can ask AI to generate integrations by connecting to RagFlow.
var ragflow = builder.AddContainer("ragflow", "infiniflow/ragflow", "v0.16.0-slim")
    .WithHttpEndpoint(targetPort: 80, name: "ragflow-ui")
    .WithHttpEndpoint(targetPort: 9380, name: "ragflow-api")
    .WithEnvironment("OLLAMA_BASE_URL", ollama.GetEndpoint("ollama-api"))
    .WithVolume("ragflow-data", "/ragflow/data")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Observability Infrastructure ──────────────────────────────────────────────
// Grafana Loki provides durable, queryable log storage for all message lifecycle
// events (traces, status, metadata). This replaces in-memory storage for real
// observability — event logs, traces, status, and metadata are all stored here.
var loki = builder.AddContainer("loki", "grafana/loki", "3.4.2")
    .WithHttpEndpoint(targetPort: 3100, name: "loki-api")
    .WithVolume("loki-data", "/loki")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Workflow Orchestration ────────────────────────────────────────────────────
// Temporal provides durable, fault-tolerant workflow orchestration.
// The auto-setup image creates the default namespace on first start.
var temporal = builder.AddContainer("temporal", "temporalio/auto-setup", "latest")
    .WithEndpoint(targetPort: 7233, name: "temporal-grpc", scheme: "http")
    .WithLifetime(ContainerLifetime.Persistent);

// Temporal Web UI for inspecting workflows, activities, and task queues.
var temporalUi = builder.AddContainer("temporal-ui", "temporalio/ui", "latest")
    .WithHttpEndpoint(targetPort: 8080, name: "temporal-ui-http")
    .WithEnvironment("TEMPORAL_ADDRESS", "temporal:7233")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Platform Services ─────────────────────────────────────────────────────────

// NATS JetStream — default queue broker for task-oriented message delivery.
// Per-subject independence avoids Head-of-Line blocking between recipients.
var nats = builder.AddContainer("nats", "nats", "latest")
    .WithArgs("--jetstream")
    .WithEndpoint(targetPort: 4222, name: "nats-client", scheme: "nats")
    .WithVolume("nats-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var gatewayApi = builder.AddProject<Projects.Gateway_Api>("gateway-api");

// Ingestion.Kafka handles Kafka streaming workloads (audit, analytics).
// Task-oriented delivery (ingestion, routing, DLQ) uses the configurable
// queue broker — NATS JetStream (default) or Apache Pulsar (Key_Shared).
var ingestionKafka = builder.AddProject<Projects.Ingestion_Kafka>("ingestion-kafka");

var workflowTemporal = builder.AddProject<Projects.Workflow_Temporal>("workflow-temporal");

var adminApi = builder.AddProject<Projects.Admin_Api>("admin-api");

var adminWeb = builder.AddProject<Projects.Admin_Web>("admin-web")
    .WithReference(adminApi);

// OpenClaw – the observability web UI – talks to Ollama for AI-powered
// message state diagnostics. Loki provides real storage for all event logs,
// traces, status, and metadata.
var openClaw = builder.AddProject<Projects.OpenClaw_Web>("openclaw")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Ollama__BaseAddress", ollama.GetEndpoint("ollama-api"))
    .WithEnvironment("Loki__BaseAddress", loki.GetEndpoint("loki-api"));

builder.Build().Run();
