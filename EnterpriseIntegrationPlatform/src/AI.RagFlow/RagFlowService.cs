using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.AI.RagFlow;

/// <summary>
/// HTTP-based implementation of <see cref="IRagFlowService"/> that communicates
/// with a self-hosted RagFlow instance via its REST API (default port 9380).
/// RagFlow handles chunking, embedding, vector storage, and retrieval internally;
/// this client sends queries and returns contextual results.
/// </summary>
public sealed class RagFlowService : IRagFlowService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RagFlowService> _logger;
    private readonly RagFlowOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initialises a new instance of <see cref="RagFlowService"/>.
    /// </summary>
    /// <param name="httpClient">
    /// An <see cref="HttpClient"/> whose <see cref="HttpClient.BaseAddress"/>
    /// points to the RagFlow API (e.g. <c>http://localhost:15380</c>).
    /// </param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">RagFlow configuration options.</param>
    public RagFlowService(HttpClient httpClient, ILogger<RagFlowService> logger, RagFlowOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> RetrieveAsync(
        string query,
        IReadOnlyList<string>? datasetIds = null,
        CancellationToken cancellationToken = default)
    {
        var body = new RetrievalRequest
        {
            Question = query,
            DatasetIds = datasetIds?.ToList(),
        };

        _logger.LogDebug("Sending retrieval request to RagFlow: {Query}", query);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/retrieval", body, JsonOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RetrievalResponse>(
                JsonOptions, cancellationToken);

            if (result?.Data?.Chunks is not { Count: > 0 })
            {
                _logger.LogInformation("RagFlow returned no chunks for query: {Query}", query);
                return string.Empty;
            }

            var context = string.Join("\n\n---\n\n",
                result.Data.Chunks.Select(c => c.Content));

            _logger.LogDebug("RagFlow returned {Count} chunks for query", result.Data.Chunks.Count);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RagFlow retrieval failed for query: {Query}", query);
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public async Task<RagFlowChatResponse> ChatAsync(
        string question,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AssistantId))
        {
            _logger.LogWarning("RagFlow AssistantId is not configured. Set RagFlow:AssistantId in configuration.");
            return new RagFlowChatResponse(
                "RagFlow assistant is not configured. Please set up a RagFlow assistant and configure the AssistantId.",
                null,
                []);
        }

        var body = new ChatRequest
        {
            Question = question,
            Stream = false,
        };

        var url = conversationId is not null
            ? $"api/v1/chats/{_options.AssistantId}/completions/{conversationId}"
            : $"api/v1/chats/{_options.AssistantId}/completions";

        _logger.LogDebug("Sending chat request to RagFlow assistant {AssistantId}: {Question}",
            _options.AssistantId, question);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                JsonOptions, cancellationToken);

            var answer = result?.Data?.Answer ?? string.Empty;
            var convId = result?.Data?.ConversationId;
            var refs = result?.Data?.References?
                .Select(r => new RagFlowReference(
                    r.Content ?? string.Empty,
                    r.DocumentName,
                    r.Score))
                .ToList() as IReadOnlyList<RagFlowReference> ?? [];

            _logger.LogDebug("RagFlow chat returned answer with {RefCount} references", refs.Count);
            return new RagFlowChatResponse(answer, convId, refs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RagFlow chat failed for question: {Question}", question);
            return new RagFlowChatResponse(
                $"RagFlow is unavailable: {ex.Message}",
                null,
                []);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RagFlowDataset>> ListDatasetsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/datasets", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DatasetListResponse>(
                JsonOptions, cancellationToken);

            return result?.Data?
                .Select(d => new RagFlowDataset(
                    d.Id ?? string.Empty,
                    d.Name ?? string.Empty,
                    d.DocumentCount))
                .ToList() as IReadOnlyList<RagFlowDataset> ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list RagFlow datasets");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/datasets?limit=1", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RagFlow health check failed");
            return false;
        }
    }

    // ── RagFlow API request/response DTOs ─────────────────────────────────────

    private sealed class RetrievalRequest
    {
        public string Question { get; init; } = string.Empty;
        [JsonPropertyName("dataset_ids")]
        public List<string>? DatasetIds { get; init; }
    }

    private sealed class RetrievalResponse
    {
        public RetrievalData? Data { get; init; }
    }

    private sealed class RetrievalData
    {
        public List<RetrievalChunk>? Chunks { get; init; }
    }

    private sealed class RetrievalChunk
    {
        public string Content { get; init; } = string.Empty;
        [JsonPropertyName("document_name")]
        public string? DocumentName { get; init; }
        public double Score { get; init; }
    }

    private sealed class ChatRequest
    {
        public string Question { get; init; } = string.Empty;
        public bool Stream { get; init; }
    }

    private sealed class ChatCompletionResponse
    {
        public ChatCompletionData? Data { get; init; }
    }

    private sealed class ChatCompletionData
    {
        public string? Answer { get; init; }
        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; init; }
        public List<ChatReference>? References { get; init; }
    }

    private sealed class ChatReference
    {
        public string? Content { get; init; }
        [JsonPropertyName("document_name")]
        public string? DocumentName { get; init; }
        public double Score { get; init; }
    }

    private sealed class DatasetListResponse
    {
        public List<DatasetItem>? Data { get; init; }
    }

    private sealed class DatasetItem
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        [JsonPropertyName("document_count")]
        public int DocumentCount { get; init; }
    }
}
