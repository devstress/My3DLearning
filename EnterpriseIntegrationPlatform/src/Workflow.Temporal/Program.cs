using EnterpriseIntegrationPlatform.Ingestion.Nats;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using EnterpriseIntegrationPlatform.Workflow.Temporal;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// ── Infrastructure for pipeline activities ─────────────────────────────
// NATS JetStream for Ack/Nack notification publishing
var natsUrl = builder.Configuration["Pipeline:NatsUrl"] ?? "nats://localhost:15222";
builder.Services.AddNatsJetStreamBroker(natsUrl);

// Cassandra for message persistence
builder.Services.AddCassandraStorage(builder.Configuration);

// Observability (Loki-backed event log + lifecycle recorder)
var lokiBaseUrl = builder.Configuration["Loki:BaseAddress"] ?? "http://localhost:15100";
builder.Services.AddPlatformObservability(lokiBaseUrl);

// ── Temporal workflows + activities ────────────────────────────────────
builder.Services.AddTemporalWorkflows(builder.Configuration);

var host = builder.Build();
host.Run();
