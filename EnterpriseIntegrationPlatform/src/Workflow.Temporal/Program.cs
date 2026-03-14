var builder = Host.CreateApplicationBuilder(args);

// Temporal worker will be configured in subsequent chunks

var host = builder.Build();
host.Run();
