var builder = DistributedApplication.CreateBuilder(args);

var gatewayApi = builder.AddProject<Projects.Gateway_Api>("gateway-api");

var ingestionKafka = builder.AddProject<Projects.Ingestion_Kafka>("ingestion-kafka");

var workflowTemporal = builder.AddProject<Projects.Workflow_Temporal>("workflow-temporal");

var adminApi = builder.AddProject<Projects.Admin_Api>("admin-api");

var adminWeb = builder.AddProject<Projects.Admin_Web>("admin-web")
    .WithReference(adminApi);

builder.Build().Run();
