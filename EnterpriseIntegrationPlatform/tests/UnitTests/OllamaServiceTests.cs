using System.Net;
using System.Text;
using System.Text.Json;
using EnterpriseIntegrationPlatform.AI.Ollama;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class OllamaServiceTests
{
    private FakeHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private ILogger<OllamaService> _logger = null!;
    private IOptions<OllamaSettings> _settings = null!;
    private OllamaService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri(OllamaServiceExtensions.DefaultBaseAddress) };
        _logger = Substitute.For<ILogger<OllamaService>>();
        _settings = Options.Create(new OllamaSettings { Model = "llama3.2" });
        _sut = new OllamaService(_httpClient, _settings, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Test]
    public async Task GenerateAsync_ServerReturnsValidJson_ReturnsResponseText()
    {
        const string expected = "Hello from Ollama!";
        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { response = expected }),
                Encoding.UTF8,
                "application/json"),
        };

        var result = await _sut.GenerateAsync("Say hello");

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateAsync_ServerReturnsError_ThrowsHttpRequestException()
    {
        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.GenerateAsync("bad prompt"));
    }

    [Test]
    public async Task GenerateAsync_ServerReturnsNullBody_ReturnsEmptyString()
    {
        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        };

        var result = await _sut.GenerateAsync("prompt");

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task AnalyseAsync_CombinesPromptsAndDelegatesToGenerate_ReturnsResult()
    {
        const string systemPrompt = "You are an analyst.";
        const string context = "Trace data here.";
        const string expectedResponse = "Analysis complete.";

        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { response = expectedResponse }),
                Encoding.UTF8,
                "application/json"),
        };

        var result = await _sut.AnalyseAsync(systemPrompt, context);

        Assert.That(result, Is.EqualTo(expectedResponse));

        var body = _handler.CapturedRequestBody;
        Assert.That(body, Does.Contain(systemPrompt));
        Assert.That(body, Does.Contain(context));
        Assert.That(body, Does.Contain("---"));
    }

    [Test]
    public async Task IsHealthyAsync_ServerReturnsOk_ReturnsTrue()
    {
        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        var result = await _sut.IsHealthyAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsHealthyAsync_ServerReturnsError_ReturnsFalse()
    {
        _handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var result = await _sut.IsHealthyAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsHealthyAsync_HttpThrowsException_ReturnsFalse()
    {
        _handler.ExceptionToThrow = new HttpRequestException("Connection refused");

        var result = await _sut.IsHealthyAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public void DefaultBaseAddress_IsCorrect()
    {
        Assert.That(OllamaServiceExtensions.DefaultBaseAddress, Is.EqualTo("http://localhost:11434"));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseToReturn { get; set; } =
            new(HttpStatusCode.OK);

        public Exception? ExceptionToThrow { get; set; }

        public string? CapturedRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return ResponseToReturn;
        }
    }
}

[TestFixture]
public class OllamaHealthCheckTests
{
    private IOllamaService _ollamaService = null!;
    private OllamaHealthCheck _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ollamaService = Substitute.For<IOllamaService>();
        _sut = new OllamaHealthCheck(_ollamaService);
    }

    [Test]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthy()
    {
        _ollamaService.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CheckHealthAsync(null!, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("reachable"));
    }

    [Test]
    public async Task CheckHealthAsync_WhenNotHealthy_ReturnsDegraded()
    {
        _ollamaService.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CheckHealthAsync(null!, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
        Assert.That(result.Description, Does.Contain("not reachable"));
    }
}
