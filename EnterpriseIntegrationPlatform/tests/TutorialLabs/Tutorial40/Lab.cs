// ============================================================================
// Tutorial 40 – RAG & Ollama / AI (Lab)
// ============================================================================
// This lab exercises IOllamaService, IRagFlowService, RagFlowChatResponse,
// OllamaSettings, and RagFlowOptions via mocks and reflection.
// ============================================================================

using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial40;

[TestFixture]
public sealed class Lab
{
    // ── IOllamaService Interface Shape (Reflection) ─────────────────────────

    [Test]
    public void IOllamaService_InterfaceShape_HasExpectedMethods()
    {
        var type = typeof(IOllamaService);

        Assert.That(type.GetMethod("GenerateAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("AnalyseAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("IsHealthyAsync"), Is.Not.Null);
    }

    // ── IRagFlowService Interface Shape (Reflection) ────────────────────────

    [Test]
    public void IRagFlowService_InterfaceShape_HasExpectedMethods()
    {
        var type = typeof(IRagFlowService);

        Assert.That(type.GetMethod("RetrieveAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("ChatAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("ListDatasetsAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("IsHealthyAsync"), Is.Not.Null);
    }

    // ── Mock IOllamaService.GenerateAsync Returns Expected Response ─────────

    [Test]
    public async Task Mock_IOllamaService_GenerateAsync_ReturnsExpected()
    {
        var ollama = Substitute.For<IOllamaService>();
        ollama.GenerateAsync(
                "What is EIP?",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("Enterprise Integration Patterns");

        var result = await ollama.GenerateAsync("What is EIP?");

        Assert.That(result, Is.EqualTo("Enterprise Integration Patterns"));
    }

    // ── Mock IRagFlowService.ChatAsync Returns RagFlowChatResponse ──────────

    [Test]
    public async Task Mock_IRagFlowService_ChatAsync_ReturnsChatResponse()
    {
        var ragFlow = Substitute.For<IRagFlowService>();
        var expectedResponse = new RagFlowChatResponse(
            Answer: "The answer is 42",
            ConversationId: "conv-123",
            References: new List<RagFlowReference>
            {
                new("Relevant passage", "doc.pdf", 0.95),
            });

        ragFlow.ChatAsync("What is the answer?", null, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await ragFlow.ChatAsync("What is the answer?");

        Assert.That(result.Answer, Is.EqualTo("The answer is 42"));
        Assert.That(result.ConversationId, Is.EqualTo("conv-123"));
        Assert.That(result.References, Has.Count.EqualTo(1));
    }

    // ── RagFlowChatResponse Record Shape ────────────────────────────────────

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

    // ── OllamaSettings Defaults ────────────────────────────────────────────

    [Test]
    public void OllamaSettings_Defaults()
    {
        var settings = new OllamaSettings();

        Assert.That(settings.Model, Is.EqualTo("llama3.2"));
    }

    // ── RagFlowOptions Defaults ─────────────────────────────────────────────

    [Test]
    public void RagFlowOptions_Defaults()
    {
        var opts = new RagFlowOptions();

        Assert.That(opts.BaseAddress, Is.EqualTo("http://localhost:15380"));
        Assert.That(opts.ApiKey, Is.Null);
        Assert.That(opts.AssistantId, Is.Null);
    }
}
