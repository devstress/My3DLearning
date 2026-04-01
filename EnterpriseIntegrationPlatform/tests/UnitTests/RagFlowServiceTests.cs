using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

using EnterpriseIntegrationPlatform.AI.RagFlow;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RagFlowOptionsTests
{
    [Test]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new RagFlowOptions();

        Assert.That(options.BaseAddress, Is.EqualTo("http://localhost:15380"));
        Assert.That(options.ApiKey, Is.Null);
        Assert.That(options.AssistantId, Is.Null);
    }

    [Test]
    public void SectionName_ShouldBeRagFlow()
    {
        Assert.That(RagFlowOptions.SectionName, Is.EqualTo("RagFlow"));
    }

    [Test]
    public void Properties_ShouldBeSettable()
    {
        var options = new RagFlowOptions
        {
            BaseAddress = "http://ragflow.prod:9380",
            ApiKey = "test-api-key",
            AssistantId = "asst-123",
        };

        Assert.That(options.BaseAddress, Is.EqualTo("http://ragflow.prod:9380"));
        Assert.That(options.ApiKey, Is.EqualTo("test-api-key"));
        Assert.That(options.AssistantId, Is.EqualTo("asst-123"));
    }
}

[TestFixture]
public class RagFlowServiceExtensionsTests
{
    [Test]
    public void DefaultBaseAddress_ShouldBeLocalhost15380()
    {
        Assert.That(RagFlowServiceExtensions.DefaultBaseAddress, Is.EqualTo("http://localhost:15380"));
    }
}

[TestFixture]
public class RagFlowServiceTests
{
    [Test]
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
        Assert.That(result, Is.False);
    }

    [Test]
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
        Assert.That(result, Is.Empty);
    }

    [Test]
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
        Assert.That(result.Answer, Does.Contain("not configured"));
        Assert.That(result.ConversationId, Is.Null);
        Assert.That(result.References, Is.Empty);
    }

    [Test]
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
        Assert.That(result.Answer, Does.Contain("unavailable"));
        Assert.That(result.ConversationId, Is.Null);
        Assert.That(result.References, Is.Empty);
    }

    [Test]
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
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RagFlowChatResponse_ShouldHoldValues()
    {
        var refs = new List<RagFlowReference>
        {
            new("chunk content", "doc.md", 0.95),
        };
        var response = new RagFlowChatResponse("answer text", "conv-123", refs);

        Assert.That(response.Answer, Is.EqualTo("answer text"));
        Assert.That(response.ConversationId, Is.EqualTo("conv-123"));
        Assert.That(response.References, Has.Count.EqualTo(1));
        Assert.That(response.References[0].Content, Is.EqualTo("chunk content"));
        Assert.That(response.References[0].DocumentName, Is.EqualTo("doc.md"));
        Assert.That(response.References[0].Score, Is.EqualTo(0.95));
    }

    [Test]
    public void RagFlowDataset_ShouldHoldValues()
    {
        var dataset = new RagFlowDataset("ds-001", "platform-docs", 42);

        Assert.That(dataset.Id, Is.EqualTo("ds-001"));
        Assert.That(dataset.Name, Is.EqualTo("platform-docs"));
        Assert.That(dataset.DocumentCount, Is.EqualTo(42));
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
