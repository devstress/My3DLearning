var builder = Host.CreateApplicationBuilder(args);

// Kafka consumer will be configured in subsequent chunks

var host = builder.Build();
host.Run();
