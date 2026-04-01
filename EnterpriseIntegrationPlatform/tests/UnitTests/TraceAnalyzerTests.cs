using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TraceAnalyzerTests
{
    private readonly IOllamaService _ollama = Substitute.For<IOllamaService>();
    private readonly TraceAnalyzer _analyzer;

    public TraceAnalyzerTests()
    {
        _analyzer = new TraceAnalyzer(_ollama, NullLogger<TraceAnalyzer>.Instance);
    }

    [Test]
    public async Task AnalyseTraceAsync_ReturnsAiResponse()
    {
        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is in Routing stage. No anomalies detected.");

        var result = await _analyzer.AnalyseTraceAsync("{\"stage\":\"Routing\"}");

        Assert.That(result, Does.Contain("Routing"));
    }

    [Test]
    public async Task AnalyseTraceAsync_ReturnsFallback_WhenOllamaFails()
    {
        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _analyzer.AnalyseTraceAsync("{\"stage\":\"Routing\"}");

        Assert.That(result, Does.Contain("unavailable"));
    }

    [Test]
    public async Task WhereIsMessageAsync_IncludesCorrelationIdInPrompt()
    {
        var correlationId = Guid.NewGuid();
        string? capturedPrompt = null;

        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Do<string>(p => capturedPrompt = p), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Found in Delivery stage.");

        var result = await _analyzer.WhereIsMessageAsync(correlationId, "{\"status\":\"InFlight\"}");

        Assert.That(capturedPrompt, Does.Contain(correlationId.ToString()));
        Assert.That(result, Does.Contain("Delivery"));
    }

    [Test]
    public async Task WhereIsMessageAsync_ReturnsFallback_WhenOllamaFails()
    {
        var correlationId = Guid.NewGuid();

        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var result = await _analyzer.WhereIsMessageAsync(correlationId, "{}");

        Assert.That(result, Does.Contain(correlationId.ToString()));
        Assert.That(result, Does.Contain("unavailable"));
    }
}
