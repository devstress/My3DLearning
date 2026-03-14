var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Kafka consumer will be configured in subsequent chunks

var host = builder.Build();
host.Run();
