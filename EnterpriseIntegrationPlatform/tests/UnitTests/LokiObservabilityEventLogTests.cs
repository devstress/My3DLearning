using System.Net;
using System.Reflection;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class LokiObservabilityEventLogTests
{
    private MockLokiHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private LokiObservabilityEventLog _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // Clear the static fallback store between tests via reflection
        var storeField = typeof(LokiObservabilityEventLog)
            .GetField("FallbackStore", BindingFlags.NonPublic | BindingFlags.Static);
        var store = (System.Collections.IList)storeField!.GetValue(null)!;
        store.Clear();

        _handler = new MockLokiHandler();
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost:3100"),
        };
        _sut = new LokiObservabilityEventLog(_httpClient,
            NullLogger<LokiObservabilityEventLog>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    private static MessageEvent CreateEvent(
        Guid? correlationId = null,
        string? businessKey = null,
        string stage = "Received")
    {
        return new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MessageType = "TestOrder",
            Source = "UnitTest",
            Stage = stage,
            Status = DeliveryStatus.Pending,
            BusinessKey = businessKey,
        };
    }

    // ── RecordAsync tests ─────────────────────────────────────────────────

    [Test]
    public async Task RecordAsync_LokiAvailable_PostsToLokiPushEndpoint()
    {
        _handler.StatusCode = HttpStatusCode.NoContent;
        var evt = CreateEvent();

        await _sut.RecordAsync(evt);

        Assert.That(_handler.Requests, Has.Count.EqualTo(1));
        Assert.That(_handler.Requests[0].RequestUri!.PathAndQuery,
            Does.Contain("loki/api/v1/push"));
    }

    [Test]
    public async Task RecordAsync_LokiUnavailable_DoesNotThrow()
    {
        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var evt = CreateEvent();

        Assert.DoesNotThrowAsync(() => _sut.RecordAsync(evt));
    }

    [Test]
    public async Task RecordAsync_AlwaysStoresInFallback()
    {
        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId: correlationId);

        await _sut.RecordAsync(evt);

        // Loki query will fail, so fallback store should be used
        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var results = await _sut.GetByCorrelationIdAsync(correlationId);
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].MessageId, Is.EqualTo(evt.MessageId));
    }

    [Test]
    public async Task RecordAsync_PostsApplicationJsonContentType()
    {
        _handler.StatusCode = HttpStatusCode.NoContent;
        var evt = CreateEvent();

        await _sut.RecordAsync(evt);

        Assert.That(_handler.Requests[0].Content!.Headers.ContentType!.MediaType,
            Is.EqualTo("application/json"));
    }

    [Test]
    public async Task RecordAsync_PayloadContainsCorrelationIdLabel()
    {
        _handler.StatusCode = HttpStatusCode.NoContent;
        _handler.CaptureBody = true;
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId: correlationId);

        await _sut.RecordAsync(evt);

        Assert.That(_handler.CapturedBodies[0],
            Does.Contain(correlationId.ToString()));
    }

    [Test]
    public async Task RecordAsync_MultipleEvents_AllStoredInFallback()
    {
        _handler.StatusCode = HttpStatusCode.NoContent;
        var correlationId = Guid.NewGuid();

        await _sut.RecordAsync(CreateEvent(correlationId: correlationId, stage: "Received"));
        await _sut.RecordAsync(CreateEvent(correlationId: correlationId, stage: "Validated"));
        await _sut.RecordAsync(CreateEvent(correlationId: correlationId, stage: "Delivered"));

        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var results = await _sut.GetByCorrelationIdAsync(correlationId);
        Assert.That(results, Has.Count.EqualTo(3));
    }

    // ── GetByCorrelationIdAsync tests ────────────────────────────────────

    [Test]
    public async Task GetByCorrelationId_LokiReturnsResults_ReturnsThem()
    {
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId: correlationId);

        // Set up Loki to return a valid query_range response
        // For push, succeed; for query, return results
        _handler.PushStatusCode = HttpStatusCode.NoContent;
        _handler.QueryStatusCode = HttpStatusCode.OK;
        _handler.QueryResponseJson = BuildLokiQueryResponse(evt);

        var results = await _sut.GetByCorrelationIdAsync(correlationId);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].MessageId, Is.EqualTo(evt.MessageId));
    }

    [Test]
    public async Task GetByCorrelationId_NoMatchingEvents_ReturnsEmpty()
    {
        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var results = await _sut.GetByCorrelationIdAsync(Guid.NewGuid());

        Assert.That(results, Is.Empty);
    }

    // ── GetByBusinessKeyAsync tests ──────────────────────────────────────

    [Test]
    public async Task GetByBusinessKey_LokiUnavailable_ReturnsFallbackResults()
    {
        var businessKey = "ORD-12345";
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId: correlationId, businessKey: businessKey);

        // First record the event (Loki push succeeds)
        _handler.StatusCode = HttpStatusCode.NoContent;
        await _sut.RecordAsync(evt);

        // Now query with Loki unavailable — should fall back to in-memory
        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var results = await _sut.GetByBusinessKeyAsync(businessKey);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BusinessKey, Is.EqualTo(businessKey));
    }

    [Test]
    public async Task GetByBusinessKey_CaseInsensitiveFallback_ReturnsResults()
    {
        var evt = CreateEvent(businessKey: "ORD-99999");
        _handler.StatusCode = HttpStatusCode.NoContent;
        await _sut.RecordAsync(evt);

        _handler.StatusCode = HttpStatusCode.InternalServerError;
        var results = await _sut.GetByBusinessKeyAsync("ord-99999");

        Assert.That(results, Has.Count.EqualTo(1));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string BuildLokiQueryResponse(MessageEvent evt)
    {
        var eventJson = JsonSerializer.Serialize(evt, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        });

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;

        // Loki values are arrays of [timestamp_string, log_line_string].
        // The log line must be a proper JSON string value (escaped).
        var escapedEventJson = JsonSerializer.Serialize(eventJson); // wraps in quotes, escapes inner

        return $$"""
        {
            "status": "success",
            "data": {
                "resultType": "streams",
                "result": [
                    {
                        "stream": {
                            "job": "eip-observability",
                            "correlation_id": "{{evt.CorrelationId}}"
                        },
                        "values": [
                            ["{{ts}}", {{escapedEventJson}}]
                        ]
                    }
                ]
            }
        }
        """;
    }

    /// <summary>
    /// Mock HTTP handler that intercepts requests and returns configurable responses.
    /// </summary>
    private sealed class MockLokiHandler : DelegatingHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode PushStatusCode { get; set; } = HttpStatusCode.NoContent;
        public HttpStatusCode QueryStatusCode { get; set; } = HttpStatusCode.InternalServerError;
        public string? QueryResponseJson { get; set; }
        public bool CaptureBody { get; set; }
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> CapturedBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (CaptureBody && request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                CapturedBodies.Add(body);
            }

            // Push requests
            if (request.RequestUri?.PathAndQuery.Contains("push") == true)
            {
                return new HttpResponseMessage(PushStatusCode != HttpStatusCode.NoContent
                    ? PushStatusCode : StatusCode);
            }

            // Query requests
            if (request.RequestUri?.PathAndQuery.Contains("query_range") == true)
            {
                if (QueryResponseJson is not null
                    && (QueryStatusCode == HttpStatusCode.OK || StatusCode == HttpStatusCode.OK))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(QueryResponseJson,
                            System.Text.Encoding.UTF8, "application/json"),
                    };
                }

                return new HttpResponseMessage(QueryStatusCode != HttpStatusCode.InternalServerError
                    ? QueryStatusCode : StatusCode);
            }

            return new HttpResponseMessage(StatusCode);
        }
    }
}
