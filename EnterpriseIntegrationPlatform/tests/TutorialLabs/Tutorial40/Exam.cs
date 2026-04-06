// ============================================================================
// Tutorial 40 – RAG & Ollama / AI (Exam)
// ============================================================================
// Coding challenges: full RAG chat flow, Ollama analysis with system prompt,
// and RagFlow dataset listing and health check.
// ============================================================================

using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial40;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full RAG Chat Flow with Mock Service ───────────────────

    [Test]
    public async Task Challenge1_FullRagChatFlow_WithMockService()
    {
        var ragFlow = Substitute.For<IRagFlowService>();

        // First chat initiates conversation
        ragFlow.ChatAsync("What is EIP?", null, Arg.Any<CancellationToken>())
            .Returns(new RagFlowChatResponse(
                "EIP stands for Enterprise Integration Patterns",
                "conv-abc",
                new List<RagFlowReference>
                {
                    new("EIP is a set of patterns...", "eip-book.pdf", 0.97),
                }));

        // Follow-up chat in same conversation
        ragFlow.ChatAsync("Give me an example", "conv-abc", Arg.Any<CancellationToken>())
            .Returns(new RagFlowChatResponse(
                "Content-Based Router is a common EIP pattern",
                "conv-abc",
                new List<RagFlowReference>
                {
                    new("A Content-Based Router inspects...", "eip-book.pdf", 0.91),
                }));

        // First question
        var first = await ragFlow.ChatAsync("What is EIP?");
        Assert.That(first.Answer, Does.Contain("Enterprise Integration Patterns"));
        Assert.That(first.ConversationId, Is.EqualTo("conv-abc"));
        Assert.That(first.References, Has.Count.EqualTo(1));

        // Follow-up with conversation context
        var followUp = await ragFlow.ChatAsync("Give me an example", first.ConversationId);
        Assert.That(followUp.Answer, Does.Contain("Content-Based Router"));
        Assert.That(followUp.ConversationId, Is.EqualTo("conv-abc"));
    }

    // ── Challenge 2: Ollama Analysis with System Prompt ─────────────────────

    [Test]
    public async Task Challenge2_OllamaAnalysis_WithSystemPrompt()
    {
        var ollama = Substitute.For<IOllamaService>();
        ollama.AnalyseAsync(
                "You are an expert in message routing patterns.",
                "The message was routed to dead-letter after 3 retries.",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("The message likely failed due to a schema validation error. " +
                     "After exhausting retries, it was moved to the dead-letter queue.");

        var analysis = await ollama.AnalyseAsync(
            "You are an expert in message routing patterns.",
            "The message was routed to dead-letter after 3 retries.");

        Assert.That(analysis, Does.Contain("dead-letter"));
        Assert.That(analysis, Does.Contain("schema validation"));

        await ollama.Received(1).AnalyseAsync(
            Arg.Is<string>(s => s.Contains("routing patterns")),
            Arg.Is<string>(s => s.Contains("3 retries")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: RagFlow Dataset Listing and Health Check ───────────────

    [Test]
    public async Task Challenge3_RagFlowDatasetListing_AndHealthCheck()
    {
        var ragFlow = Substitute.For<IRagFlowService>();

        ragFlow.IsHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        ragFlow.ListDatasetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RagFlowDataset>
            {
                new("ds-1", "EIP Patterns", 42),
                new("ds-2", "System Management Docs", 15),
                new("ds-3", "API Reference", 108),
            });

        // Verify health
        var healthy = await ragFlow.IsHealthyAsync();
        Assert.That(healthy, Is.True);

        // List datasets
        var datasets = await ragFlow.ListDatasetsAsync();
        Assert.That(datasets, Has.Count.EqualTo(3));
        Assert.That(datasets[0].Id, Is.EqualTo("ds-1"));
        Assert.That(datasets[0].Name, Is.EqualTo("EIP Patterns"));
        Assert.That(datasets[0].DocumentCount, Is.EqualTo(42));
        Assert.That(datasets[1].Name, Is.EqualTo("System Management Docs"));
        Assert.That(datasets[2].DocumentCount, Is.EqualTo(108));
    }
}
