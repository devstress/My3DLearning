using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class TraceAnalyzerTests
{
    private readonly IOllamaService _ollama = Substitute.For<IOllamaService>();
    private readonly TraceAnalyzer _analyzer;

    public TraceAnalyzerTests()
    {
        _analyzer = new TraceAnalyzer(_ollama, NullLogger<TraceAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyseTraceAsync_ReturnsAiResponse()
    {
        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message is in Routing stage. No anomalies detected.");

        var result = await _analyzer.AnalyseTraceAsync("{\"stage\":\"Routing\"}");

        result.Should().Contain("Routing");
    }

    [Fact]
    public async Task AnalyseTraceAsync_ReturnsFallback_WhenOllamaFails()
    {
        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _analyzer.AnalyseTraceAsync("{\"stage\":\"Routing\"}");

        result.Should().Contain("unavailable");
    }

    [Fact]
    public async Task WhereIsMessageAsync_IncludesCorrelationIdInPrompt()
    {
        var correlationId = Guid.NewGuid();
        string? capturedPrompt = null;

        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Do<string>(p => capturedPrompt = p), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Found in Delivery stage.");

        var result = await _analyzer.WhereIsMessageAsync(correlationId, "{\"status\":\"InFlight\"}");

        capturedPrompt.Should().Contain(correlationId.ToString());
        result.Should().Contain("Delivery");
    }

    [Fact]
    public async Task WhereIsMessageAsync_ReturnsFallback_WhenOllamaFails()
    {
        var correlationId = Guid.NewGuid();

        _ollama.AnalyseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var result = await _analyzer.WhereIsMessageAsync(correlationId, "{}");

        result.Should().Contain(correlationId.ToString());
        result.Should().Contain("unavailable");
    }
}
