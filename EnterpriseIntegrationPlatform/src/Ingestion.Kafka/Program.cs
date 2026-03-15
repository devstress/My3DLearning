using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Kafka;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Bind broker configuration from the "Broker" section.
builder.Services.AddBrokerOptions(builder.Configuration);

// Kafka consumer/producer for streaming workloads (audit, analytics).
// Task-oriented delivery uses the configurable queue broker (NATS JetStream
// or Apache Pulsar Key_Shared) — configured via BrokerOptions.
var bootstrapServers = builder.Configuration["Broker:ConnectionString"] ?? "localhost:9092";
builder.Services.AddKafkaBroker(bootstrapServers);

var host = builder.Build();
host.Run();
