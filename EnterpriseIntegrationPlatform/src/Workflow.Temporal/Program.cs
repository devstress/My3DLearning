using EnterpriseIntegrationPlatform.Workflow.Temporal;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddTemporalWorkflows(builder.Configuration);

var host = builder.Build();
host.Run();
