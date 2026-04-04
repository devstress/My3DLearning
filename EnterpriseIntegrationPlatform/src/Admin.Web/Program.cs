using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Admin API HTTP Client ─────────────────────────────────────────────────────
// The Admin.Web frontend proxies all API requests to Admin.Api.
// BaseAddress and ApiKey are configured via Aspire environment variables or appsettings.
var adminApiBaseAddress = builder.Configuration["AdminApi:BaseAddress"] ?? "http://localhost:5180";
var adminApiKey = builder.Configuration["AdminApi:ApiKey"] ?? "";

builder.Services.AddHttpClient("AdminApi", client =>
{
    client.BaseAddress = new Uri(adminApiBaseAddress);
    if (!string.IsNullOrEmpty(adminApiKey))
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", adminApiKey);
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();

// ── Admin API Proxy Endpoints ─────────────────────────────────────────────────
// The Vue 3 frontend calls these local endpoints; the server proxies them to Admin.Api
// with the configured API key. This avoids exposing the API key to the browser.

var proxy = app.MapGroup("/api/admin");

proxy.MapGet("/status", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/status", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetStatus");

proxy.MapGet("/messages/correlation/{correlationId}", async (
    string correlationId, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync($"/api/admin/messages/correlation/{correlationId}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetMessagesByCorrelation");

proxy.MapGet("/messages/{messageId}", async (
    string messageId, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync($"/api/admin/messages/{messageId}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetMessageById");

proxy.MapGet("/faults/correlation/{correlationId}", async (
    string correlationId, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync($"/api/admin/faults/correlation/{correlationId}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetFaultsByCorrelation");

proxy.MapPost("/dlq/resubmit", async (
    HttpRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
    var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/api/admin/dlq/resubmit", httpContent, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyDlqResubmit");

proxy.MapGet("/throttle/policies", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    try
    {
        var response = await client.GetAsync("/api/admin/throttle/policies", ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException)
    {
        return Results.Json(Array.Empty<object>(), statusCode: 200);
    }
}).WithName("ProxyGetThrottlePolicies");

proxy.MapGet("/throttle/policies/{policyId}", async (
    string policyId, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync($"/api/admin/throttle/policies/{policyId}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetThrottlePolicy");

proxy.MapPut("/throttle/policies", async (
    HttpRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
    var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await client.PutAsync("/api/admin/throttle/policies", httpContent, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxySetThrottlePolicy");

proxy.MapDelete("/throttle/policies/{policyId}", async (
    string policyId, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.DeleteAsync($"/api/admin/throttle/policies/{policyId}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyDeleteThrottlePolicy");

proxy.MapGet("/ratelimit/status", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/ratelimit/status", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetRateLimitStatus");

proxy.MapGet("/dr/regions", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/dr/regions", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetDrRegions");

proxy.MapPost("/dr/drills", async (
    HttpRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
    var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/api/admin/dr/drills", httpContent, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyRunDrDrill");

proxy.MapGet("/dr/drills/history", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/dr/drills/history", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetDrDrillHistory");

proxy.MapPost("/profiling/snapshot", async (
    HttpRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var label = request.Query["label"].FirstOrDefault();
    var url = label is not null ? $"/api/admin/profiling/snapshot?label={Uri.EscapeDataString(label)}" : "/api/admin/profiling/snapshot";
    var response = await client.PostAsync(url, null, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyCaptureSnapshot");

proxy.MapGet("/profiling/snapshot/latest", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/profiling/snapshot/latest", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetLatestSnapshot");

proxy.MapGet("/profiling/gc", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync("/api/admin/profiling/gc", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetGcSnapshot");

proxy.MapGet("/events/business/{businessKey}", async (
    string businessKey, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("AdminApi");
    var response = await client.GetAsync($"/api/admin/events/business/{businessKey}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
}).WithName("ProxyGetEventsByBusinessKey");

// ── Serve the Vue 3 SPA from Vite build output (wwwroot/) ─────────────────────
// In production, the Vite-built assets are served from wwwroot/.
// The SPA fallback ensures client-side routing works for all non-API paths.

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
