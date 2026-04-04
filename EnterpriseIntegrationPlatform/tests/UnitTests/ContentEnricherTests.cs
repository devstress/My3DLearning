using System.Net;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
public sealed class ContentEnricherTests
{
    private IHttpClientFactory _httpClientFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
    }

    private ContentEnricher BuildEnricher(
        ContentEnricherOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        respond ??= _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"enriched":true}"""),
        };

        var handler = new FakeHttpMessageHandler(respond);
        var client = new HttpClient(handler);
        _httpClientFactory.CreateClient("ContentEnricher").Returns(client);

        return new ContentEnricher(
            _httpClientFactory,
            Options.Create(options),
            NullLogger<ContentEnricher>.Instance);
    }

    private static ContentEnricherOptions DefaultOptions(
        string endpoint = "https://api.example.com/customers/{key}",
        string lookupPath = "order.customerId",
        string mergePath = "customer") =>
        new()
        {
            EndpointUrlTemplate = endpoint,
            LookupKeyPath = lookupPath,
            MergeTargetPath = mergePath,
        };

    [Test]
    public async Task EnrichAsync_HttpLookupSuccess_MergesDataAtTargetPath()
    {
        var options = DefaultOptions();
        var enricher = BuildEnricher(options, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"Alice","tier":"gold"}"""),
        });

        var payload = """{"order":{"id":1,"customerId":"C-42"}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("Alice"));
        Assert.That(doc.RootElement.GetProperty("customer").GetProperty("tier").GetString(),
            Is.EqualTo("gold"));
        // Original data preserved.
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task EnrichAsync_MissingLookupKey_FallbackEnabled_ReturnsOriginal()
    {
        var options = DefaultOptions(lookupPath: "order.missingField");
        var enricher = BuildEnricher(options);

        var payload = """{"order":{"id":1}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public void EnrichAsync_MissingLookupKey_FallbackDisabled_Throws()
    {
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "missing.path",
            MergeTargetPath = "data",
            FallbackOnFailure = false,
        };
        var enricher = BuildEnricher(options);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => enricher.EnrichAsync("""{"order":{"id":1}}""", Guid.NewGuid()));
    }

    [Test]
    public async Task EnrichAsync_HttpError_FallbackEnabled_ReturnsOriginal()
    {
        var options = DefaultOptions();
        var enricher = BuildEnricher(options,
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var payload = """{"order":{"id":1,"customerId":"C-42"}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task EnrichAsync_HttpError_FallbackWithStaticValue_MergesFallback()
    {
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "order.customerId",
            MergeTargetPath = "customer",
            FallbackOnFailure = true,
            FallbackValue = """{"name":"Unknown","tier":"default"}""",
        };
        var enricher = BuildEnricher(options,
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var payload = """{"order":{"id":1,"customerId":"C-42"}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("Unknown"));
    }

    [Test]
    public void EnrichAsync_HttpError_FallbackDisabled_Throws()
    {
        var options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/{key}",
            LookupKeyPath = "order.customerId",
            MergeTargetPath = "data",
            FallbackOnFailure = false,
        };
        var enricher = BuildEnricher(options,
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.ThrowsAsync<HttpRequestException>(
            () => enricher.EnrichAsync("""{"order":{"customerId":"C-42"}}""", Guid.NewGuid()));
    }

    [Test]
    public async Task EnrichAsync_NumericLookupKey_SubstitutedCorrectly()
    {
        string? capturedUrl = null;
        var options = DefaultOptions(lookupPath: "order.id");
        var enricher = BuildEnricher(options, req =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"found"}"""),
            };
        });

        await enricher.EnrichAsync("""{"order":{"id":42,"customerId":"C-42"}}""", Guid.NewGuid());

        Assert.That(capturedUrl, Does.Contain("/customers/42"));
    }

    [Test]
    public async Task EnrichAsync_NestedMergePath_CreatesIntermediateObjects()
    {
        var options = DefaultOptions(mergePath: "details.enrichment.data");
        var enricher = BuildEnricher(options, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"value":"enriched"}"""),
        });

        var payload = """{"order":{"id":1,"customerId":"C-42"}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement
                .GetProperty("details")
                .GetProperty("enrichment")
                .GetProperty("data")
                .GetProperty("value")
                .GetString(),
            Is.EqualTo("enriched"));
    }

    [Test]
    public void EnrichAsync_NullPayload_Throws()
    {
        var enricher = BuildEnricher(DefaultOptions());
        Assert.ThrowsAsync<ArgumentNullException>(
            () => enricher.EnrichAsync(null!, Guid.NewGuid()));
    }

    [Test]
    public void EnrichAsync_InvalidJson_Throws()
    {
        var enricher = BuildEnricher(DefaultOptions());
        Assert.That(
            () => enricher.EnrichAsync("not-json", Guid.NewGuid()),
            Throws.InstanceOf<JsonException>());
    }

    [Test]
    public async Task EnrichAsync_CancellationRequested_Throws()
    {
        var options = DefaultOptions();
        var enricher = BuildEnricher(options);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Assert.ThrowsAsync<TaskCanceledException>(
            () => enricher.EnrichAsync("""{"order":{"customerId":"C-42"}}""", Guid.NewGuid(), cts.Token));
    }

    [Test]
    public async Task EnrichAsync_ExistingMergeTargetOverwritten()
    {
        var options = DefaultOptions();
        var enricher = BuildEnricher(options, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"New"}"""),
        });

        var payload = """{"order":{"id":1,"customerId":"C-42"},"customer":{"name":"Old"}}""";
        var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("New"));
    }

    // ───── Helper ─────

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
            _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_respond(request));
        }
    }
}
