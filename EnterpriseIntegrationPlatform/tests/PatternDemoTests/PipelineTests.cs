using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Pipes and Filters pattern.
/// Chains multiple processing steps into a sequential pipeline.
/// BizTalk equivalent: Receive Pipeline (Decode → Disassemble → Validate → ResolveParty)
///                      and Send Pipeline (Pre-Assemble → Assemble → Encode).
/// EIP: Pipes and Filters (p. 70)
/// </summary>
public class PipelineTests
{
    private record OrderPayload(string CustomerId, decimal Amount, string? ValidatedBy);

    [Fact]
    public async Task Executes_Steps_InSequence()
    {
        var steps = new List<string>();

        var pipeline = new Pipeline<OrderPayload>()
            .AddStep(async (env, ct) =>
            {
                steps.Add("validate");
                return env; // pass through
            })
            .AddStep(async (env, ct) =>
            {
                steps.Add("enrich");
                // Enrich with validation stamp
                return IntegrationEnvelope<OrderPayload>.Create(
                    env.Payload with { ValidatedBy = "PipelineStep" },
                    env.Source, env.MessageType, env.CorrelationId);
            })
            .AddStep(async (env, ct) =>
            {
                steps.Add("route");
                return env;
            });

        var input = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("CUST-01", 100, null), "ERP", "OrderCreated");

        var result = await pipeline.ExecuteAsync(input);

        steps.Should().ContainInOrder("validate", "enrich", "route");
        result.Payload.ValidatedBy.Should().Be("PipelineStep");
    }

    [Fact]
    public async Task Empty_Pipeline_ReturnsInputUnchanged()
    {
        var pipeline = new Pipeline<OrderPayload>();

        var input = IntegrationEnvelope<OrderPayload>.Create(
            new OrderPayload("CUST-01", 100, null), "ERP", "OrderCreated");

        var result = await pipeline.ExecuteAsync(input);

        result.Should().BeSameAs(input);
    }
}
