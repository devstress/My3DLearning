// ============================================================================
// Tutorial 40 – RAG & Ollama / AI (Exam)
// ============================================================================
// E2E challenges: full RAG chat flow with MockEndpoint, Ollama analysis with
// system prompt, and RagFlow dataset listing and health check.
// ============================================================================

using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using EnterpriseIntegrationPlatform.Contracts;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial40;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullRagChatFlow_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-rag-in");
        await using var output = new MockEndpoint("exam-rag-out");

        var ragFlow = Substitute.For<IRagFlowService>();
        ragFlow.ChatAsync("What is EIP?", null, Arg.Any<CancellationToken>())
            .Returns(new RagFlowChatResponse(
                "EIP stands for Enterprise Integration Patterns", "conv-abc",
                new List<RagFlowReference>
                {
                    new("EIP is a set of patterns...", "eip-book.pdf", 0.97),
                }));

        ragFlow.ChatAsync("Give me an example", "conv-abc", Arg.Any<CancellationToken>())
            .Returns(new RagFlowChatResponse(
                "Content-Based Router is a common EIP pattern", "conv-abc",
                new List<RagFlowReference>
                {
                    new("A Content-Based Router inspects...", "eip-book.pdf", 0.91),
                }));

        await input.SubscribeAsync<string>("rag-topic", "rag-group",
            async envelope =>
            {
                var chatResult = await ragFlow.ChatAsync(envelope.Payload);
                var enriched = envelope with
                {
                    Metadata = new Dictionary<string, string>(envelope.Metadata)
                    {
                        ["rag-answer"] = chatResult.Answer,
                        ["rag-conv-id"] = chatResult.ConversationId ?? "",
                    },
                };
                await output.PublishAsync(enriched, "rag-results");
            });

        // First question
        var q1 = IntegrationEnvelope<string>.Create("What is EIP?", "UserSvc", "rag.query");
        await input.SendAsync(q1);

        output.AssertReceivedOnTopic("rag-results", 1);
        var r1 = output.GetReceived<string>();
        Assert.That(r1.Metadata["rag-answer"], Does.Contain("Enterprise Integration Patterns"));

        // Follow-up (direct call to verify conversation continuity)
        var followUp = await ragFlow.ChatAsync("Give me an example", "conv-abc");
        Assert.That(followUp.Answer, Does.Contain("Content-Based Router"));
        Assert.That(followUp.ConversationId, Is.EqualTo("conv-abc"));
    }

    [Test]
    public async Task Challenge2_OllamaAnalysis_WithSystemPrompt()
    {
        var ollama = Substitute.For<IOllamaService>();
        ollama.AnalyseAsync(
                "You are an expert in message routing patterns.",
                "The message was routed to dead-letter after 3 retries.",
                Arg.Any<string>(), Arg.Any<CancellationToken>())
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
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Challenge3_RagFlowDatasetListing_AndHealthCheck()
    {
        var ragFlow = Substitute.For<IRagFlowService>();
        ragFlow.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        ragFlow.ListDatasetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RagFlowDataset>
            {
                new("ds-1", "EIP Patterns", 42),
                new("ds-2", "System Management Docs", 15),
                new("ds-3", "API Reference", 108),
            });

        var healthy = await ragFlow.IsHealthyAsync();
        Assert.That(healthy, Is.True);

        var datasets = await ragFlow.ListDatasetsAsync();
        Assert.That(datasets, Has.Count.EqualTo(3));
        Assert.That(datasets[0].Id, Is.EqualTo("ds-1"));
        Assert.That(datasets[0].Name, Is.EqualTo("EIP Patterns"));
        Assert.That(datasets[0].DocumentCount, Is.EqualTo(42));
        Assert.That(datasets[1].Name, Is.EqualTo("System Management Docs"));
        Assert.That(datasets[2].DocumentCount, Is.EqualTo(108));
    }
}
