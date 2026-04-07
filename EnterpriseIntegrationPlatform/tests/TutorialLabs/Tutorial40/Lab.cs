// ============================================================================
// Tutorial 40 – RAG & Ollama / AI (Lab)
// ============================================================================
// EIP Pattern: AI-enriched integration.
// E2E: Mock IOllamaService and IRagFlowService with MockOllamaService/MockRagFlowService, wire
// MockEndpoint to simulate AI-enriched message pipelines.
// ============================================================================

using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial40;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _input = null!;
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new MockEndpoint("ai-in");
        _output = new MockEndpoint("ai-out");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _input.DisposeAsync();
        await _output.DisposeAsync();
    }


    // ── 1. AI Service Interactions ───────────────────────────────────

    [Test]
    public async Task Ollama_GenerateAsync_ReturnsExpected()
    {
        var ollama = new MockOllamaService()
            .WithGenerateResponse("What is EIP?", "Enterprise Integration Patterns");

        var result = await ollama.GenerateAsync("What is EIP?");

        Assert.That(result, Is.EqualTo("Enterprise Integration Patterns"));
    }

    [Test]
    public async Task RagFlow_ChatAsync_ReturnsChatResponse()
    {
        var expected = new RagFlowChatResponse(
            "The answer is 42", "conv-123",
            new List<RagFlowReference> { new("Relevant passage", "doc.pdf", 0.95) });

        var ragFlow = new MockRagFlowService()
            .WithChatResponse("What is the answer?", null, expected);

        var result = await ragFlow.ChatAsync("What is the answer?");

        Assert.That(result.Answer, Is.EqualTo("The answer is 42"));
        Assert.That(result.ConversationId, Is.EqualTo("conv-123"));
        Assert.That(result.References, Has.Count.EqualTo(1));
    }


    // ── 2. Configuration & Data Contracts ────────────────────────────

    [Test]
    public void OllamaSettings_Defaults()
    {
        var settings = new OllamaSettings();
        Assert.That(settings.Model, Is.EqualTo("llama3.2"));
    }

    [Test]
    public void RagFlowOptions_Defaults()
    {
        var opts = new RagFlowOptions();
        Assert.That(opts.BaseAddress, Is.EqualTo("http://localhost:15380"));
        Assert.That(opts.ApiKey, Is.Null);
        Assert.That(opts.AssistantId, Is.Null);
    }

    [Test]
    public void RagFlowChatResponse_RecordShape()
    {
        var refs = new List<RagFlowReference>
        {
            new("passage 1", "file1.pdf", 0.9),
            new("passage 2", "file2.pdf", 0.8),
        };

        var response = new RagFlowChatResponse("Answer text", "conv-1", refs);

        Assert.That(response.Answer, Is.EqualTo("Answer text"));
        Assert.That(response.ConversationId, Is.EqualTo("conv-1"));
        Assert.That(response.References, Has.Count.EqualTo(2));
        Assert.That(response.References[0].DocumentName, Is.EqualTo("file1.pdf"));
        Assert.That(response.References[1].Score, Is.EqualTo(0.8));
    }


    // ── 3. End-to-End AI Pipeline ────────────────────────────────────

    [Test]
    public async Task E2E_MockEndpoint_AiEnrichedPipeline()
    {
        var ollama = new MockOllamaService()
            .WithDefaultResponse("Message processed successfully through all stages");

        // Subscribe: receive envelope, enrich with AI analysis, publish to output
        await _input.SubscribeAsync<string>("ai-topic", "ai-group",
            async envelope =>
            {
                var analysis = await ollama.AnalyseAsync(
                    "Analyse this message", envelope.Payload);

                var enriched = envelope with
                {
                    Metadata = new Dictionary<string, string>(envelope.Metadata)
                    {
                        ["ai-analysis"] = analysis,
                    },
                };
                await _output.PublishAsync(enriched, "enriched-topic");
            });

        var env = IntegrationEnvelope<string>.Create(
            "Order data for analysis", "OrderSvc", "order.placed");
        await _input.SendAsync(env);

        _output.AssertReceivedOnTopic("enriched-topic", 1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Metadata["ai-analysis"],
            Is.EqualTo("Message processed successfully through all stages"));
    }
}
