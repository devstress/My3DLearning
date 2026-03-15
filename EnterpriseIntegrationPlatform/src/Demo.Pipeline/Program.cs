using EnterpriseIntegrationPlatform.Demo.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDemoPipeline(builder.Configuration);

var host = builder.Build();
host.Run();
