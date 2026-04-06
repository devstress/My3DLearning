// ============================================================================
// Tutorial 19 – Content Filter (Lab)
// ============================================================================
// This lab exercises the JsonPathFilterStep and the ContentFilter — the
// pattern that strips a message down to only the fields the next consumer
// needs.  You will test path-based filtering, missing-path handling,
// nested-property extraction, and pipeline integration.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial19;

[TestFixture]
public sealed class Lab
{
    // ── Basic JsonPathFilterStep ────────────────────────────────────────────

    [Test]
    public async Task FilterStep_RetainsOnlySpecifiedPaths()
    {
        var step = new JsonPathFilterStep(new[] { "name", "age" });
        var context = new TransformContext(
            """{"name":"Alice","age":30,"email":"a@b.com","role":"admin"}""",
            "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("name", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("role", out _), Is.False);
    }

    // ── Nested Property Extraction ──────────────────────────────────────────

    [Test]
    public async Task FilterStep_NestedPath_ExtractsNestedProperty()
    {
        var step = new JsonPathFilterStep(new[] { "order.id", "customer.name" });
        var payload = """
            {
                "order": {"id": "ORD-1", "total": 100},
                "customer": {"name": "Bob", "email": "bob@test.com"},
                "internal": "secret"
            }
            """;

        var context = new TransformContext(payload, "application/json");
        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetString(),
            Is.EqualTo("ORD-1"));
        Assert.That(doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
            Is.EqualTo("Bob"));
        Assert.That(doc.RootElement.TryGetProperty("internal", out _), Is.False);
    }

    // ── Missing Paths Are Silently Skipped ──────────────────────────────────

    [Test]
    public async Task FilterStep_MissingPath_SilentlySkipped()
    {
        var step = new JsonPathFilterStep(new[] { "name", "nonexistent" });
        var context = new TransformContext(
            """{"name":"Alice","age":30}""", "application/json");

        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.TryGetProperty("name", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("nonexistent", out _), Is.False);
    }

    // ── Metadata Written by Step ────────────────────────────────────────────

    [Test]
    public async Task FilterStep_SetsAppliedMetadata()
    {
        var step = new JsonPathFilterStep(new[] { "id" });
        var context = new TransformContext("""{"id":1,"extra":"x"}""", "application/json");

        var result = await step.ExecuteAsync(context);

        Assert.That(result.Metadata.ContainsKey("Step.JsonPathFilter.Applied"), Is.True);
        Assert.That(result.Metadata["Step.JsonPathFilter.Applied"], Is.EqualTo("true"));
    }

    // ── Pipeline Integration ────────────────────────────────────────────────

    [Test]
    public async Task FilterStep_InPipeline_FiltersPayload()
    {
        var filterStep = new JsonPathFilterStep(new[] { "order.id", "order.total" });
        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new ITransformStep[] { filterStep }, options,
            NullLogger<TransformPipeline>.Instance);

        var payload = """
            {"order":{"id":"ORD-5","total":250,"items":3},"customer":{"name":"Eve"}}
            """.Trim();

        var result = await pipeline.ExecuteAsync(payload, "application/json");

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetString(),
            Is.EqualTo("ORD-5"));
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("total").GetInt32(),
            Is.EqualTo(250));
        Assert.That(doc.RootElement.TryGetProperty("customer", out _), Is.False);
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    // ── ContentFilter Class (Direct Usage) ──────────────────────────────────

    [Test]
    public async Task ContentFilter_RetainsOnlyKeepPaths()
    {
        var filter = new ContentFilter(NullLogger<ContentFilter>.Instance);

        var payload = """
            {"user":"Alice","age":30,"email":"a@b.com","role":"admin","secret":"x"}
            """.Trim();

        var result = await filter.FilterAsync(payload, new[] { "user", "age" });

        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.TryGetProperty("user", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("age", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("secret", out _), Is.False);
    }
}
