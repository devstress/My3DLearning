// ============================================================================
// Tutorial 34 – HTTP Connector (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — send to custom destination_ publishes result
//   🟡 Intermediate  — token caching lifecycle
//   🔴 Advanced      — multiple connectors_ independent results
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial34;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_SendToCustomDestination_PublishesResult()
    {
        await using var output = new MockEndpoint("exam-http-dest");
        // TODO: Create a MockHttpConnector with appropriate configuration
        dynamic http = null!;

        // TODO: Create a HttpConnectorAdapter with appropriate configuration
        dynamic adapter = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await adapter.SendAsync(...)
        dynamic result = null!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("order-http"));

        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("order-results", 1);
    }

    [Test]
    public async Task Intermediate_TokenCachingLifecycle()
    {
        await using var output = new MockEndpoint("exam-token");
        // TODO: Create a FakeTimeProvider with appropriate configuration
        dynamic time = null!;
        // TODO: Create a InMemoryTokenCache with appropriate configuration
        dynamic cache = null!;

        cache.SetToken("auth-endpoint", "token-abc", TimeSpan.FromSeconds(30));
        Assert.That(cache.TryGetToken("auth-endpoint", out var t1), Is.True);
        Assert.That(t1, Is.EqualTo("token-abc"));

        time.Advance(TimeSpan.FromSeconds(31));
        Assert.That(cache.TryGetToken("auth-endpoint", out _), Is.False);

        cache.SetToken("auth-endpoint", "token-xyz", TimeSpan.FromSeconds(60));
        Assert.That(cache.TryGetToken("auth-endpoint", out var t2), Is.True);
        Assert.That(t2, Is.EqualTo("token-xyz"));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("token-events", 1);
    }

    [Test]
    public async Task Advanced_MultipleConnectors_IndependentResults()
    {
        await using var output = new MockEndpoint("exam-multi");
        var connectors = new[] { "api-a", "api-b" };

        foreach (var name in connectors)
        {
            // TODO: Create a MockHttpConnector with appropriate configuration
            dynamic http = null!;

            // TODO: Create a HttpConnectorAdapter with appropriate configuration
            dynamic adapter = null!;

            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic envelope = null!;
            // TODO: var result = await adapter.SendAsync(...)
            dynamic result = null!;
            Assert.That(result.Success, Is.True);

            // TODO: await output.PublishAsync(...)
        }

        output.AssertReceivedCount(2);
        output.AssertReceivedOnTopic("results.api-a", 1);
        output.AssertReceivedOnTopic("results.api-b", 1);
    }
}
#endif
