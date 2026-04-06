// ============================================================================
// MockHttpConnector – In-memory HTTP connector for testing
// ============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IHttpConnector"/> that captures
/// HTTP requests and returns configurable responses.
/// </summary>
public sealed class MockHttpConnector : IHttpConnector
{
    private readonly ConcurrentQueue<HttpCallRecord> _calls = new();
    private readonly Dictionary<string, object> _responses = new();
    private object? _defaultResponse;
    private Func<string, HttpMethod, Exception>? _failureInjector;

    /// <summary>All HTTP calls recorded.</summary>
    public IReadOnlyList<HttpCallRecord> Calls => _calls.ToArray();

    /// <summary>Number of calls recorded.</summary>
    public int CallCount => _calls.Count;

    /// <summary>Sets the default response for any URL.</summary>
    public MockHttpConnector WithDefaultResponse<TResponse>(TResponse response)
    {
        _defaultResponse = response;
        return this;
    }

    /// <summary>Sets a specific response for a given relative URL.</summary>
    public MockHttpConnector WithResponse<TResponse>(string relativeUrl, TResponse response)
    {
        _responses[relativeUrl] = response!;
        return this;
    }

    /// <summary>Injects a failure for calls matching the predicate.</summary>
    public MockHttpConnector WithFailure(Func<string, HttpMethod, Exception> failureInjector)
    {
        _failureInjector = failureInjector;
        return this;
    }

    /// <summary>Injects a failure for all calls.</summary>
    public MockHttpConnector WithFailure(Exception ex)
    {
        _failureInjector = (_, _) => ex;
        return this;
    }

    public Task<TResponse> SendAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        CancellationToken ct)
    {
        _calls.Enqueue(new HttpCallRecord(envelope!, relativeUrl, method, null, DateTimeOffset.UtcNow));

        if (_failureInjector is not null)
            throw _failureInjector(relativeUrl, method);

        if (_responses.TryGetValue(relativeUrl, out var specific))
            return Task.FromResult((TResponse)specific);

        if (_defaultResponse is not null)
            return Task.FromResult((TResponse)_defaultResponse);

        if (typeof(TResponse) == typeof(JsonElement))
        {
            var defaultJson = JsonDocument.Parse("{}").RootElement;
            return Task.FromResult((TResponse)(object)defaultJson);
        }

        return Task.FromResult(default(TResponse)!);
    }

    public Task<TResponse> SendWithTokenAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string tokenEndpoint,
        string tokenRequestBody,
        string tokenHeaderName,
        CancellationToken ct)
    {
        _calls.Enqueue(new HttpCallRecord(envelope!, relativeUrl, method, tokenEndpoint, DateTimeOffset.UtcNow));
        return SendAsync<TPayload, TResponse>(envelope, relativeUrl, method, ct);
    }

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record HttpCallRecord(
        object Envelope,
        string RelativeUrl,
        HttpMethod Method,
        string? TokenEndpoint,
        DateTimeOffset CalledAt);
}
