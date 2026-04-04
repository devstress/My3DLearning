using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
public class RagFlowHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthy()
    {
        var ragFlow = Substitute.For<IRagFlowService>();
        ragFlow.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        var healthCheck = new RagFlowHealthCheck(ragFlow);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Description, Does.Contain("reachable"));
    }

    [Test]
    public async Task CheckHealthAsync_WhenUnhealthy_ReturnsDegraded()
    {
        var ragFlow = Substitute.For<IRagFlowService>();
        ragFlow.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);
        var healthCheck = new RagFlowHealthCheck(ragFlow);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
        Assert.That(result.Description, Does.Contain("not reachable"));
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

    [Test]
    public void RagFlowReference_ShouldHoldValues()
    {
        var reference = new RagFlowReference("some content", "doc.md", 0.87);

        Assert.That(reference.Content, Is.EqualTo("some content"));
        Assert.That(reference.DocumentName, Is.EqualTo("doc.md"));
        Assert.That(reference.Score, Is.EqualTo(0.87));
    }

    [Test]
    public async Task IsHealthyAsync_WhenServerReturnsOk_ReturnsTrue()
    {
        var handler = new SuccessHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[]}", Encoding.UTF8, "application/json"),
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var service = new RagFlowService(httpClient, logger, new RagFlowOptions());

        var result = await service.IsHealthyAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RetrieveAsync_WhenSuccess_ReturnsJoinedChunks()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new
            {
                chunks = new[]
                {
                    new { content = "chunk one", document_name = "doc1.md", score = 0.9 },
                    new { content = "chunk two", document_name = "doc2.md", score = 0.8 },
                },
            },
        });
        var handler = new SuccessHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var service = new RagFlowService(httpClient, logger, new RagFlowOptions());

        var result = await service.RetrieveAsync("test query");

        Assert.That(result, Is.EqualTo("chunk one\n\n---\n\nchunk two"));
    }

    [Test]
    public async Task ListDatasetsAsync_WhenSuccess_ReturnsDatasets()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { id = "ds-1", name = "Dataset One", document_count = 10 },
                new { id = "ds-2", name = "Dataset Two", document_count = 20 },
            },
        });
        var handler = new SuccessHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var service = new RagFlowService(httpClient, logger, new RagFlowOptions());

        var result = await service.ListDatasetsAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("ds-1"));
        Assert.That(result[0].Name, Is.EqualTo("Dataset One"));
        Assert.That(result[0].DocumentCount, Is.EqualTo(10));
        Assert.That(result[1].Id, Is.EqualTo("ds-2"));
    }

    [Test]
    public async Task ChatAsync_WhenSuccess_ReturnsAnswerAndReferences()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new
            {
                answer = "The answer is 42",
                conversation_id = "conv-abc",
                references = new[]
                {
                    new { content = "ref content", document_name = "ref.md", score = 0.95 },
                },
            },
        });
        var handler = new SuccessHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:15380") };
        var logger = Substitute.For<ILogger<RagFlowService>>();
        var options = new RagFlowOptions { AssistantId = "asst-123" };
        var service = new RagFlowService(httpClient, logger, options);

        var result = await service.ChatAsync("What is the answer?");

        Assert.That(result.Answer, Is.EqualTo("The answer is 42"));
        Assert.That(result.ConversationId, Is.EqualTo("conv-abc"));
        Assert.That(result.References, Has.Count.EqualTo(1));
        Assert.That(result.References[0].Content, Is.EqualTo("ref content"));
        Assert.That(result.References[0].DocumentName, Is.EqualTo("ref.md"));
        Assert.That(result.References[0].Score, Is.EqualTo(0.95));
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

    /// <summary>HTTP handler that returns a configurable response for testing success paths.</summary>
    private sealed class SuccessHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public SuccessHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
