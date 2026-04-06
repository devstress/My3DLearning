// ============================================================================
// Tutorial 19 – Content Filter (Exam)
// ============================================================================
// Coding challenges: build a PII-stripping filter, compose filter + regex
// pipeline, and test the standalone ContentFilter with deeply nested JSON.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial19;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: PII Stripping Filter ───────────────────────────────────

    [Test]
    public async Task Challenge1_PiiStripping_RemovesSensitiveFields()
    {
        // Given a payload with PII fields (email, ssn, phone), use a
        // JsonPathFilterStep to retain only the non-sensitive fields.
        var step = new JsonPathFilterStep(new[]
        {
            "order.id", "order.total", "order.currency",
        });

        var payload = """
            {
                "order": {"id": "ORD-77", "total": 500, "currency": "USD"},
                "customer": {"name": "Alice", "email": "alice@secret.com", "ssn": "123-45-6789"},
                "internal": {"traceId": "abc-123"}
            }
            """;

        var context = new TransformContext(payload, "application/json");
        var result = await step.ExecuteAsync(context);

        using var doc = JsonDocument.Parse(result.Payload);
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("id").GetString(),
            Is.EqualTo("ORD-77"));
        Assert.That(doc.RootElement.GetProperty("order").GetProperty("total").GetInt32(),
            Is.EqualTo(500));
        Assert.That(doc.RootElement.TryGetProperty("customer", out _), Is.False);
        Assert.That(doc.RootElement.TryGetProperty("internal", out _), Is.False);
    }

    // ── Challenge 2: Filter + Regex Pipeline ────────────────────────────────

    [Test]
    public async Task Challenge2_FilterThenRegex_CombinedPipeline()
    {
        // First filter to keep only "message" and "level", then regex-replace
        // to redact any numeric sequences longer than 4 digits.
        var filterStep = new JsonPathFilterStep(new[] { "message", "level" });
        var regexStep = new RegexReplaceStep(@"\d{5,}", "[REDACTED]");

        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new ITransformStep[] { filterStep, regexStep }, options,
            NullLogger<TransformPipeline>.Instance);

        var payload = """
            {"message":"Error code 123456 occurred","level":"error","secret":"password123"}
            """.Trim();

        var result = await pipeline.ExecuteAsync(payload, "application/json");

        Assert.That(result.Payload, Does.Contain("[REDACTED]"));
        Assert.That(result.Payload, Does.Not.Contain("123456"));
        Assert.That(result.Payload, Does.Not.Contain("secret"));
        Assert.That(result.Payload, Does.Not.Contain("password123"));
        Assert.That(result.StepsApplied, Is.EqualTo(2));
    }

    // ── Challenge 3: Deeply Nested Extraction ───────────────────────────────

    [Test]
    public async Task Challenge3_DeeplyNestedPaths_ExtractedCorrectly()
    {
        // Use the standalone ContentFilter to extract deeply nested paths from
        // a complex JSON payload.
        var filter = new ContentFilter(NullLogger<ContentFilter>.Instance);

        var payload = """
            {
                "company": {
                    "name": "Acme Corp",
                    "address": {
                        "street": "123 Main St",
                        "city": "Springfield",
                        "zip": "62701"
                    },
                    "ceo": "Jane Doe"
                },
                "revenue": 1000000,
                "confidential": {"salaries": [100,200,300]}
            }
            """;

        var result = await filter.FilterAsync(payload, new[]
        {
            "company.name",
            "company.address.city",
            "revenue",
        });

        using var doc = JsonDocument.Parse(result);
        Assert.That(
            doc.RootElement.GetProperty("company").GetProperty("name").GetString(),
            Is.EqualTo("Acme Corp"));
        Assert.That(
            doc.RootElement.GetProperty("company").GetProperty("address").GetProperty("city").GetString(),
            Is.EqualTo("Springfield"));
        Assert.That(doc.RootElement.GetProperty("revenue").GetInt32(), Is.EqualTo(1000000));
        Assert.That(doc.RootElement.TryGetProperty("confidential", out _), Is.False);
    }
}
