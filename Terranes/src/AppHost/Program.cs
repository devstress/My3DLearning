var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Platform_Api>("platform-api");

builder.AddViteApp("web-vue", "../Web.Vue")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
