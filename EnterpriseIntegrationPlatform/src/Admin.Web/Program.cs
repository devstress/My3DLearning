var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Enterprise Integration Platform - Admin"));

app.Run();
