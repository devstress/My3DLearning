var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
