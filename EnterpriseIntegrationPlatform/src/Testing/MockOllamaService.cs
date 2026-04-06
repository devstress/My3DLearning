// ============================================================================
// MockOllamaService – In-memory Ollama/LLM service for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.AI.Ollama;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IOllamaService"/> that returns
/// configurable responses and captures all calls.
/// </summary>
public sealed class MockOllamaService : IOllamaService
{
    private readonly ConcurrentQueue<OllamaCallRecord> _calls = new();
    private readonly Dictionary<string, string> _generateResponses = new();
    private readonly Dictionary<string, string> _analyseResponses = new();
    private string _defaultResponse = "Mock AI response";
    private bool _isHealthy = true;

    /// <summary>All calls recorded.</summary>
    public IReadOnlyList<OllamaCallRecord> Calls => _calls.ToArray();

    /// <summary>Number of calls recorded.</summary>
    public int CallCount => _calls.Count;

    /// <summary>Sets the default response for unmatched prompts.</summary>
    public MockOllamaService WithDefaultResponse(string response)
    {
        _defaultResponse = response;
        return this;
    }

    /// <summary>Sets a specific response for an exact prompt.</summary>
    public MockOllamaService WithGenerateResponse(string prompt, string response)
    {
        _generateResponses[prompt] = response;
        return this;
    }

    /// <summary>Sets a specific response for an analyse call matching the context.</summary>
    public MockOllamaService WithAnalyseResponse(string context, string response)
    {
        _analyseResponses[context] = response;
        return this;
    }

    /// <summary>Sets the health status.</summary>
    public MockOllamaService WithHealthy(bool isHealthy)
    {
        _isHealthy = isHealthy;
        return this;
    }

    public Task<string> GenerateAsync(string prompt, string model = "llama3.2", CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new OllamaCallRecord("Generate", prompt, null, model));
        return Task.FromResult(
            _generateResponses.TryGetValue(prompt, out var resp)
                ? resp
                : _defaultResponse);
    }

    public Task<string> AnalyseAsync(string systemPrompt, string context, string model = "llama3.2", CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new OllamaCallRecord("Analyse", context, systemPrompt, model));
        return Task.FromResult(
            _analyseResponses.TryGetValue(context, out var resp)
                ? resp
                : _defaultResponse);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_isHealthy);

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record OllamaCallRecord(string Operation, string Prompt, string? SystemPrompt, string Model);
}
