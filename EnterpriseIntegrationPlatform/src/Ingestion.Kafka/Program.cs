var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Kafka consumer/producer for streaming workloads (audit, analytics).
// Task-oriented delivery uses the configurable queue broker (NATS JetStream
// or Apache Pulsar Key_Shared) — configured in subsequent chunks.

var host = builder.Build();
host.Run();
