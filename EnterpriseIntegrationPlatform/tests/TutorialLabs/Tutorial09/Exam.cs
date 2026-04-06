// ============================================================================
// Tutorial 09 – Content-Based Router (Exam)
// ============================================================================
// E2E challenges: multi-rule regional routing, payload-based routing with
// JsonElement, and multi-message batch routing verification via MockEndpoint.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial09;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_RegionalRouting_MatchesAndFallsBack()
    {
        await using var output = new MockEndpoint("regional");
        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule { Priority = 1, Name = "US-East",
                    FieldName = "Metadata.region", Operator = RoutingOperator.Equals,
                    Value = "us-east", TargetTopic = "fulfilment.us-east" },
                new RoutingRule { Priority = 2, Name = "EU-West",
                    FieldName = "Metadata.region", Operator = RoutingOperator.Equals,
                    Value = "eu-west", TargetTopic = "fulfilment.eu-west" },
            ],
            DefaultTopic = "fulfilment.global",
        });
        var router = new ContentBasedRouter(
            output, options, NullLogger<ContentBasedRouter>.Instance);

        var usOrder = IntegrationEnvelope<string>.Create("o1", "svc", "order.created") with
            { Metadata = new Dictionary<string, string> { ["region"] = "us-east" } };
        var euOrder = IntegrationEnvelope<string>.Create("o2", "svc", "order.created") with
            { Metadata = new Dictionary<string, string> { ["region"] = "eu-west" } };
        var unknownOrder = IntegrationEnvelope<string>.Create("o3", "svc", "order.created") with
            { Metadata = new Dictionary<string, string> { ["region"] = "af-south" } };

        Assert.That((await router.RouteAsync(usOrder)).TargetTopic, Is.EqualTo("fulfilment.us-east"));
        Assert.That((await router.RouteAsync(euOrder)).TargetTopic, Is.EqualTo("fulfilment.eu-west"));
        Assert.That((await router.RouteAsync(unknownOrder)).IsDefault, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("fulfilment.us-east", 1);
        output.AssertReceivedOnTopic("fulfilment.eu-west", 1);
        output.AssertReceivedOnTopic("fulfilment.global", 1);
    }

    [Test]
    public async Task Challenge2_PayloadRouting_JsonElementField()
    {
        await using var output = new MockEndpoint("payload");
        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule { Priority = 1, Name = "Urgent",
                    FieldName = "Payload.status", Operator = RoutingOperator.Equals,
                    Value = "urgent", TargetTopic = "urgent-processing" },
                new RoutingRule { Priority = 2, Name = "Normal",
                    FieldName = "Payload.status", Operator = RoutingOperator.Equals,
                    Value = "normal", TargetTopic = "normal-processing" },
            ],
            DefaultTopic = "default-processing",
        });
        var router = new ContentBasedRouter(
            output, options, NullLogger<ContentBasedRouter>.Instance);

        var urgentJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"urgent\",\"amount\":5000}");
        var normalJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"normal\",\"amount\":50}");
        var unknownJson = JsonSerializer.Deserialize<JsonElement>(
            "{\"status\":\"backorder\",\"amount\":10}");

        var d1 = await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(urgentJson, "svc", "order"));
        var d2 = await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(normalJson, "svc", "order"));
        var d3 = await router.RouteAsync(IntegrationEnvelope<JsonElement>.Create(unknownJson, "svc", "order"));

        Assert.That(d1.TargetTopic, Is.EqualTo("urgent-processing"));
        Assert.That(d2.TargetTopic, Is.EqualTo("normal-processing"));
        Assert.That(d3.IsDefault, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("urgent-processing", 1);
        output.AssertReceivedOnTopic("normal-processing", 1);
        output.AssertReceivedOnTopic("default-processing", 1);
    }

    [Test]
    public async Task Challenge3_BatchRouting_MultipleMessagesVerifyTopics()
    {
        await using var output = new MockEndpoint("batch");
        var options = Options.Create(new RouterOptions
        {
            Rules =
            [
                new RoutingRule { Priority = 1,
                    FieldName = "MessageType", Operator = RoutingOperator.StartsWith,
                    Value = "order", TargetTopic = "orders" },
                new RoutingRule { Priority = 2,
                    FieldName = "MessageType", Operator = RoutingOperator.StartsWith,
                    Value = "payment", TargetTopic = "payments" },
            ],
            DefaultTopic = "other",
        });
        var router = new ContentBasedRouter(
            output, options, NullLogger<ContentBasedRouter>.Instance);

        await router.RouteAsync(IntegrationEnvelope<string>.Create("d1", "svc", "order.created"));
        await router.RouteAsync(IntegrationEnvelope<string>.Create("d2", "svc", "order.shipped"));
        await router.RouteAsync(IntegrationEnvelope<string>.Create("d3", "svc", "payment.received"));
        await router.RouteAsync(IntegrationEnvelope<string>.Create("d4", "svc", "inventory.updated"));

        output.AssertReceivedCount(4);
        output.AssertReceivedOnTopic("orders", 2);
        output.AssertReceivedOnTopic("payments", 1);
        output.AssertReceivedOnTopic("other", 1);
        Assert.That(output.GetReceivedTopics(), Has.Count.EqualTo(3));
    }
}
