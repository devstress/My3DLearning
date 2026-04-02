using EnterpriseIntegrationPlatform.Gateway.Api;
using EnterpriseIntegrationPlatform.Gateway.Api.Middleware;
using EnterpriseIntegrationPlatform.Gateway.Api.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Gateway Services ──────────────────────────────────────────────────────────
builder.Services.AddGateway(builder.Configuration);

// ── HTTP clients for downstream proxying ──────────────────────────────────────
builder.Services.AddHttpClient("downstream");

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.MapDefaultEndpoints();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Health Aggregation ────────────────────────────────────────────────────────
app.MapHealthChecks("/gateway/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// ── Versioned API Routes ──────────────────────────────────────────────────────
// All routes under /api/v{version}/ are resolved and proxied to downstream services.

app.MapMethods("/api/v{version}/{**rest}", ["GET", "POST", "PUT", "PATCH", "DELETE"], async (
    string version,
    string? rest,
    HttpContext context,
    IRouteResolver routeResolver,
    IHttpClientFactory httpClientFactory) =>
{
    var incomingPath = context.Request.Path.Value ?? $"/api/v{version}/{rest}";
    var downstreamUrl = routeResolver.Resolve(incomingPath);

    if (downstreamUrl is null)
    {
        return Results.NotFound(new { error = "No downstream route matched.", path = incomingPath });
    }

    // Append query string if present
    if (context.Request.QueryString.HasValue)
    {
        downstreamUrl += context.Request.QueryString.Value;
    }

    var client = httpClientFactory.CreateClient("downstream");

    using var downstreamRequest = new HttpRequestMessage(
        new HttpMethod(context.Request.Method),
        downstreamUrl);

    // Forward headers (except Host)
    foreach (var header in context.Request.Headers)
    {
        if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        downstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    // Forward body for methods that have one
    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        downstreamRequest.Content = new StreamContent(context.Request.Body);
        if (context.Request.ContentType is not null)
        {
            downstreamRequest.Content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    // Forward correlation ID
    if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var correlationId))
    {
        downstreamRequest.Headers.TryAddWithoutValidation(
            CorrelationIdMiddleware.HeaderName,
            correlationId?.ToString());
    }

    try
    {
        using var downstreamResponse = await client.SendAsync(
            downstreamRequest,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);

        context.Response.StatusCode = (int)downstreamResponse.StatusCode;

        foreach (var header in downstreamResponse.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in downstreamResponse.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // Remove transfer-encoding when we're copying content directly
        context.Response.Headers.Remove("transfer-encoding");

        await downstreamResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        return Results.Empty;
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            detail: $"Downstream service unavailable: {ex.Message}",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem(
            detail: "Downstream service request timed out.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
})
.WithName("GatewayProxy")
.AllowAnonymous();

// ── Gateway Info ──────────────────────────────────────────────────────────────
app.MapGet("/", [AllowAnonymous] () => Results.Ok(new
{
    service = "Enterprise Integration Platform – API Gateway",
    version = "1.0.0",
    docs = "/gateway/health",
}));

app.Run();
