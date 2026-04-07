// ============================================================================
// TestHttpServer – In-process ASP.NET minimal API test server for Tutorial 34
// ============================================================================
// Provides a real HTTP endpoint with configurable responses, replacing
// MockHttpConnector with an actual network round-trip.
// ============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Real in-process HTTP server using ASP.NET minimal APIs.
/// Configurable response handlers allow tests to control what the
/// endpoint returns while exercising real HTTP round-trips.
/// </summary>
public sealed class TestHttpServer : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly ConcurrentDictionary<string, Func<HttpContext, Task>> _handlers = new();
    private readonly ConcurrentQueue<HttpCallRecord> _calls = new();

    public string BaseUrl { get; private set; } = "";
    public bool IsRunning { get; private set; }

    /// <summary>All HTTP calls received by this server.</summary>
    public IReadOnlyList<HttpCallRecord> Calls => _calls.ToArray();

    /// <summary>Number of calls received.</summary>
    public int CallCount => _calls.Count;

    /// <summary>
    /// Registers a handler for a specific path.
    /// The handler receives the full HttpContext and can write a response.
    /// </summary>
    public TestHttpServer WithHandler(string path, Func<HttpContext, Task> handler)
    {
        _handlers[path] = handler;
        return this;
    }

    /// <summary>
    /// Registers a JSON response for a specific path.
    /// </summary>
    public TestHttpServer WithJsonResponse<T>(string path, T response)
    {
        _handlers[path] = async ctx =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
        };
        return this;
    }

    /// <summary>
    /// Registers a default JSON response for all unmatched paths.
    /// </summary>
    public TestHttpServer WithDefaultJsonResponse<T>(T response)
    {
        _handlers["__default__"] = async ctx =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
        };
        return this;
    }

    /// <summary>Starts the HTTP server on a random port.</summary>
    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _app = builder.Build();

        _app.MapFallback(async context =>
        {
            var path = context.Request.Path.Value ?? "/";
            var body = "";
            if (context.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(context.Request.Body);
                body = await reader.ReadToEndAsync();
            }

            _calls.Enqueue(new HttpCallRecord(
                path,
                context.Request.Method,
                body,
                DateTimeOffset.UtcNow));

            if (_handlers.TryGetValue(path, out var handler))
            {
                await handler(context);
            }
            else if (_handlers.TryGetValue("__default__", out var defaultHandler))
            {
                await defaultHandler(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Not found");
            }
        });

        await _app.StartAsync();
        BaseUrl = _app.Urls.First();
        IsRunning = true;
    }

    /// <summary>Clears all recorded calls.</summary>
    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
            IsRunning = false;
        }
    }

    public sealed record HttpCallRecord(
        string Path,
        string Method,
        string Body,
        DateTimeOffset CalledAt);
}
