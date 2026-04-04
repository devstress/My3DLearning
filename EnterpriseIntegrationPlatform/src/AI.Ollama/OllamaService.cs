using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.AI.Ollama;

/// <summary>
/// HTTP-based implementation of <see cref="IOllamaService"/> that communicates
/// with a local or remote Ollama instance via its REST API.
/// </summary>
public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initialises a new instance of <see cref="OllamaService"/>.
    /// </summary>
    /// <param name="httpClient">
    /// An <see cref="HttpClient"/> whose <see cref="HttpClient.BaseAddress"/>
    /// points to the Ollama API (e.g. <c>http://localhost:15434</c>).
    /// </param>
    /// <param name="logger">Logger instance.</param>
    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string prompt,
        string model = "llama3.2",
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaGenerateRequest { Model = model, Prompt = prompt, Stream = false };

        _logger.LogDebug("Sending generate request to Ollama model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            "api/generate", request, JsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
            JsonOptions, cancellationToken);

        return result?.Response ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string> AnalyseAsync(
        string systemPrompt,
        string context,
        string model = "llama3.2",
        CancellationToken cancellationToken = default)
    {
        var combinedPrompt = $"{systemPrompt}\n\n---\n\n{context}";
        return await GenerateAsync(combinedPrompt, model, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a short timeout for health checks so the UI doesn't hang
            // when Ollama is unavailable (e.g. in CI or before Ollama starts).
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync(string.Empty, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed");
            return false;
        }
    }

    private sealed class OllamaGenerateRequest
    {
        public string Model { get; init; } = "llama3.2";
        public string Prompt { get; init; } = string.Empty;
        public bool Stream { get; init; }
    }

    private sealed class OllamaGenerateResponse
    {
        public string Response { get; init; } = string.Empty;
    }
}
