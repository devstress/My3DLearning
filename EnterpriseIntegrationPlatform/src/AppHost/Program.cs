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

// ── Platform Services ─────────────────────────────────────────────────────────

var gatewayApi = builder.AddProject<Projects.Gateway_Api>("gateway-api");

var ingestionKafka = builder.AddProject<Projects.Ingestion_Kafka>("ingestion-kafka");

var workflowTemporal = builder.AddProject<Projects.Workflow_Temporal>("workflow-temporal");

var adminApi = builder.AddProject<Projects.Admin_Api>("admin-api");

var adminWeb = builder.AddProject<Projects.Admin_Web>("admin-web")
    .WithReference(adminApi);

// OpenClaw – the observability web UI – talks to Ollama for AI-powered
// message state diagnostics and references the Ollama endpoint.
var openClaw = builder.AddProject<Projects.OpenClaw_Web>("openclaw")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Ollama__BaseAddress", ollama.GetEndpoint("ollama-api"));

builder.Build().Run();
