// ============================================================================
// MockRagFlowService – In-memory RAG/retrieval service for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.AI.RagFlow;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IRagFlowService"/> that returns
/// configurable responses and captures all calls.
/// </summary>
public sealed class MockRagFlowService : IRagFlowService
{
    private readonly ConcurrentQueue<RagFlowCallRecord> _calls = new();
    private readonly Dictionary<string, RagFlowChatResponse> _chatResponses = new();
    private readonly List<RagFlowDataset> _datasets = new();
    private string _defaultRetrieveResponse = "Mock retrieval response";
    private bool _isHealthy = true;

    /// <summary>All calls recorded.</summary>
    public IReadOnlyList<RagFlowCallRecord> Calls => _calls.ToArray();

    /// <summary>Sets the default retrieve response.</summary>
    public MockRagFlowService WithRetrieveResponse(string response)
    {
        _defaultRetrieveResponse = response;
        return this;
    }

    /// <summary>Sets a chat response for a specific question.</summary>
    public MockRagFlowService WithChatResponse(string question, RagFlowChatResponse response)
    {
        _chatResponses[question] = response;
        return this;
    }

    /// <summary>Sets a chat response matching a conversation ID.</summary>
    public MockRagFlowService WithChatResponse(string question, string? conversationId, RagFlowChatResponse response)
    {
        var key = $"{question}|{conversationId ?? "null"}";
        _chatResponses[key] = response;
        return this;
    }

    /// <summary>Adds datasets to return from ListDatasetsAsync.</summary>
    public MockRagFlowService WithDatasets(params RagFlowDataset[] datasets)
    {
        _datasets.AddRange(datasets);
        return this;
    }

    /// <summary>Sets health status.</summary>
    public MockRagFlowService WithHealthy(bool isHealthy)
    {
        _isHealthy = isHealthy;
        return this;
    }

    public Task<string> RetrieveAsync(string query, IReadOnlyList<string>? datasetIds = null, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new RagFlowCallRecord("Retrieve", query, null));
        return Task.FromResult(_defaultRetrieveResponse);
    }

    public Task<RagFlowChatResponse> ChatAsync(string question, string? conversationId = null, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new RagFlowCallRecord("Chat", question, conversationId));

        // Try exact match with conversation ID first
        var keyWithConv = $"{question}|{conversationId ?? "null"}";
        if (_chatResponses.TryGetValue(keyWithConv, out var convResp))
            return Task.FromResult(convResp);

        // Try question-only match
        if (_chatResponses.TryGetValue(question, out var resp))
            return Task.FromResult(resp);

        return Task.FromResult(new RagFlowChatResponse("Mock answer", conversationId, []));
    }

    public Task<IReadOnlyList<RagFlowDataset>> ListDatasetsAsync(CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new RagFlowCallRecord("ListDatasets", null, null));
        return Task.FromResult<IReadOnlyList<RagFlowDataset>>(_datasets.AsReadOnly());
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_isHealthy);

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record RagFlowCallRecord(string Operation, string? Query, string? ConversationId);
}
