using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

using EnterpriseIntegrationPlatform.AI.RagFlow;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class RagFlowOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new RagFlowOptions();

        options.BaseAddress.Should().Be("http://localhost:15380");
        options.ApiKey.Should().BeNull();
        options.AssistantId.Should().BeNull();
    }

    [Fact]
    public void SectionName_ShouldBeRagFlow()
    {
        RagFlowOptions.SectionName.Should().Be("RagFlow");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var options = new RagFlowOptions
        {
            BaseAddress = "http://ragflow.prod:9380",
            ApiKey = "test-api-key",
            AssistantId = "asst-123",
        };

        options.BaseAddress.Should().Be("http://ragflow.prod:9380");
        options.ApiKey.Should().Be("test-api-key");
        options.AssistantId.Should().Be("asst-123");
    }
}

public class RagFlowServiceExtensionsTests
{
    [Fact]
    public void DefaultBaseAddress_ShouldBeLocalhost15380()
    {
        RagFlowServiceExtensions.DefaultBaseAddress.Should().Be("http://localhost:15380");
    }
}

public class RagFlowServiceTests
{
    [Fact]
    public async Task IsHealthyAsync_WhenHttpFails_ShouldReturnFalse()
    {
        // Arrange — HttpClient with a handler that throws
        var handler = new FailingHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions();
        var service = new RagFlowService(httpClient, logger, options);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RetrieveAsync_WhenHttpFails_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new FailingHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions();
        var service = new RagFlowService(httpClient, logger, options);

        // Act
        var result = await service.RetrieveAsync("test query");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatAsync_WhenNoAssistantId_ShouldReturnConfigurationMessage()
    {
        // Arrange
        var handler = new FailingHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions { AssistantId = null };
        var service = new RagFlowService(httpClient, logger, options);

        // Act
        var result = await service.ChatAsync("generate something");

        // Assert
        result.Answer.Should().Contain("not configured");
        result.ConversationId.Should().BeNull();
        result.References.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatAsync_WhenHttpFails_ShouldReturnErrorMessage()
    {
        // Arrange
        var handler = new FailingHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions { AssistantId = "asst-123" };
        var service = new RagFlowService(httpClient, logger, options);

        // Act
        var result = await service.ChatAsync("generate something");

        // Assert
        result.Answer.Should().Contain("unavailable");
        result.ConversationId.Should().BeNull();
        result.References.Should().BeEmpty();
    }

    [Fact]
    public async Task ListDatasetsAsync_WhenHttpFails_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new FailingHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions();
        var service = new RagFlowService(httpClient, logger, options);

        // Act
        var result = await service.ListDatasetsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RagFlowChatResponse_ShouldHoldValues()
    {
        var refs = new List<RagFlowReference>
        {
            new("chunk content", "doc.md", 0.95),
        };
        var response = new RagFlowChatResponse("answer text", "conv-123", refs);

        response.Answer.Should().Be("answer text");
        response.ConversationId.Should().Be("conv-123");
        response.References.Should().HaveCount(1);
        response.References[0].Content.Should().Be("chunk content");
        response.References[0].DocumentName.Should().Be("doc.md");
        response.References[0].Score.Should().Be(0.95);
    }

    [Fact]
    public void RagFlowDataset_ShouldHoldValues()
    {
        var dataset = new RagFlowDataset("ds-001", "platform-docs", 42);

        dataset.Id.Should().Be("ds-001");
        dataset.Name.Should().Be("platform-docs");
        dataset.DocumentCount.Should().Be(42);
    }

    /// <summary>HTTP handler that always throws to simulate unavailable service.</summary>
    private sealed class FailingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Connection refused");
        }
    }
}
