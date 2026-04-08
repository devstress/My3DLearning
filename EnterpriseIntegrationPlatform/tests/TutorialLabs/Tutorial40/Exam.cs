// ============================================================================
// Tutorial 40 – RAG & Ollama / AI (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full rag chat flow_ through mock endpoint
//   🟡 Intermediate  — ollama analysis_ with system prompt
//   🔴 Advanced      — rag flow dataset listing_ and health check
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.AI.Ollama;
using EnterpriseIntegrationPlatform.AI.RagFlow;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial40;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullRagChatFlow_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-rag-in");
        await using var output = new MockEndpoint("exam-rag-out");

        // TODO: Create a MockRagFlowService with appropriate configuration
        dynamic ragFlow = null!;

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
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic q1 = null!;
        // TODO: await input.SendAsync(...)

        output.AssertReceivedOnTopic("rag-results", 1);
        var r1 = output.GetReceived<string>();
        Assert.That(r1.Metadata["rag-answer"], Does.Contain("Enterprise Integration Patterns"));

        // Follow-up (direct call to verify conversation continuity)
        // TODO: var followUp = await ragFlow.ChatAsync(...)
        dynamic followUp = null!;
        Assert.That(followUp.Answer, Does.Contain("Content-Based Router"));
        Assert.That(followUp.ConversationId, Is.EqualTo("conv-abc"));
    }

    [Test]
    public async Task Intermediate_OllamaAnalysis_WithSystemPrompt()
    {
        // TODO: Create a MockOllamaService with appropriate configuration
        dynamic ollama = null!;

        // TODO: var analysis = await ollama.AnalyseAsync(...)
        dynamic analysis = null!;

        Assert.That(analysis, Does.Contain("dead-letter"));
        Assert.That(analysis, Does.Contain("schema validation"));

        Assert.That(ollama.CallCount, Is.EqualTo(1));
        Assert.That(ollama.Calls[0].SystemPrompt, Does.Contain("routing patterns"));
        Assert.That(ollama.Calls[0].Prompt, Does.Contain("3 retries"));
    }

    [Test]
    public async Task Advanced_RagFlowDatasetListing_AndHealthCheck()
    {
        // TODO: Create a MockRagFlowService with appropriate configuration
        dynamic ragFlow = null!;

        // TODO: var healthy = await ragFlow.IsHealthyAsync(...)
        dynamic healthy = null!;
        Assert.That(healthy, Is.True);

        // TODO: var datasets = await ragFlow.ListDatasetsAsync(...)
        dynamic datasets = null!;
        Assert.That(datasets, Has.Count.EqualTo(3));
        Assert.That(datasets[0].Id, Is.EqualTo("ds-1"));
        Assert.That(datasets[0].Name, Is.EqualTo("EIP Patterns"));
        Assert.That(datasets[0].DocumentCount, Is.EqualTo(42));
        Assert.That(datasets[1].Name, Is.EqualTo("System Management Docs"));
        Assert.That(datasets[2].DocumentCount, Is.EqualTo(108));
    }
}
#endif
