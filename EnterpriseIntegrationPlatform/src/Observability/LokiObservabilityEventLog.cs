using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Observability;

/// <summary>
/// Grafana Loki–backed implementation of <see cref="IObservabilityEventLog"/>.
/// Pushes message lifecycle events to Loki via its HTTP push API and queries
/// them back using LogQL label selectors.
/// <para>
/// Loki stores events as structured log entries with labels for
/// <c>correlation_id</c>, <c>business_key</c>, <c>message_type</c>, and <c>stage</c>.
/// The full <see cref="MessageEvent"/> is serialised as JSON in the log line.
/// </para>
/// </summary>
public sealed class LokiObservabilityEventLog : IObservabilityEventLog
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LokiObservabilityEventLog> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Initialises a new instance of <see cref="LokiObservabilityEventLog"/>.
    /// </summary>
    /// <param name="httpClient">
    /// An <see cref="HttpClient"/> whose <see cref="HttpClient.BaseAddress"/>
    /// points to the Loki HTTP API (e.g. <c>http://localhost:15100</c>).
    /// </param>
    /// <param name="logger">Logger instance.</param>
    public LokiObservabilityEventLog(HttpClient httpClient, ILogger<LokiObservabilityEventLog> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default)
    {
        var timestampNs = checked(messageEvent.RecordedAt.ToUnixTimeMilliseconds() * 1_000_000);
        var logLine = JsonSerializer.Serialize(messageEvent, SerializerOptions);

        var labels = new Dictionary<string, string>
        {
            ["job"] = "eip-observability",
            ["correlation_id"] = messageEvent.CorrelationId.ToString(),
            ["message_type"] = messageEvent.MessageType,
            ["stage"] = messageEvent.Stage,
            ["status"] = messageEvent.Status.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(messageEvent.BusinessKey))
        {
            labels["business_key"] = messageEvent.BusinessKey;
        }

        var payload = new LokiPushPayload
        {
            Streams =
            [
                new LokiStream
                {
                    Stream = labels,
                    Values = [[timestampNs.ToString(), logLine]],
                },
            ],
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("loki/api/v1/push", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Pushed event {EventId} to Loki for CorrelationId={CorrelationId}",
                messageEvent.EventId, messageEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push event {EventId} to Loki", messageEvent.EventId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey,
        CancellationToken cancellationToken = default)
    {
        // LogQL: select all streams where business_key matches (case-insensitive via regex)
        var query = $"{{job=\"eip-observability\", business_key=~\"(?i)^{EscapeLogQL(businessKey)}$\"}}";
        return await QueryEventsAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var query = $"{{job=\"eip-observability\", correlation_id=\"{correlationId}\"}}";
        return await QueryEventsAsync(query, cancellationToken);
    }

    private async Task<IReadOnlyList<MessageEvent>> QueryEventsAsync(
        string logqlQuery,
        CancellationToken cancellationToken)
    {
        // Query the last 7 days by default; label selectors already filter to
        // the exact correlation/business-key so data volume is bounded.
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-7);
        var startNs = checked(start.ToUnixTimeMilliseconds() * 1_000_000);
        var endNs = checked(end.ToUnixTimeMilliseconds() * 1_000_000);

        var url = $"loki/api/v1/query_range?query={Uri.EscapeDataString(logqlQuery)}" +
                  $"&start={startNs}&end={endNs}&limit=5000&direction=forward";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LokiQueryResult>(
                SerializerOptions, cancellationToken);

            if (result?.Data?.Result is null || result.Data.Result.Count == 0)
            {
                return Array.Empty<MessageEvent>();
            }

            var events = new List<MessageEvent>();
            foreach (var stream in result.Data.Result)
            {
                foreach (var value in stream.Values)
                {
                    if (value.Count >= 2)
                    {
                        var logLine = value[1];
                        var evt = JsonSerializer.Deserialize<MessageEvent>(logLine, SerializerOptions);
                        if (evt is not null)
                        {
                            events.Add(evt);
                        }
                    }
                }
            }

            return events.OrderBy(e => e.RecordedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Loki with LogQL: {Query}", logqlQuery);
            return Array.Empty<MessageEvent>();
        }
    }

    /// <summary>Escapes special regex characters in a LogQL label value.</summary>
    private static string EscapeLogQL(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(".", "\\.")
            .Replace("*", "\\*")
            .Replace("+", "\\+")
            .Replace("?", "\\?")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("|", "\\|")
            .Replace("^", "\\^")
            .Replace("$", "\\$");
    }

    // ── Loki API DTOs ─────────────────────────────────────────────────────────

    private sealed class LokiPushPayload
    {
        public List<LokiStream> Streams { get; init; } = [];
    }

    private sealed class LokiStream
    {
        public Dictionary<string, string> Stream { get; init; } = new();
        public List<List<string>> Values { get; init; } = [];
    }

    private sealed class LokiQueryResult
    {
        public string Status { get; init; } = string.Empty;
        public LokiQueryData? Data { get; init; }
    }

    private sealed class LokiQueryData
    {
        public string ResultType { get; init; } = string.Empty;
        public List<LokiResultStream>? Result { get; init; }
    }

    private sealed class LokiResultStream
    {
        public Dictionary<string, string> Stream { get; init; } = new();
        public List<List<string>> Values { get; init; } = [];
    }
}
