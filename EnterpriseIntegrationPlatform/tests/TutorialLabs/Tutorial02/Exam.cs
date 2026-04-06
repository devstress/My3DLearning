// ============================================================================
// Tutorial 02 – Environment Setup (Exam)
// ============================================================================
// Coding challenges that test your understanding of the platform's
// configuration types and well-known header constants.
// ============================================================================

using System.Reflection;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Validate BrokerOptions Properties ──────────────────────

    [Test]
    public void Challenge1_BrokerOptions_HasBrokerTypeProperty()
    {
        var property = typeof(BrokerOptions).GetProperty("BrokerType");
        Assert.That(property, Is.Not.Null, "BrokerOptions must have a BrokerType property");
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(BrokerType)));
        Assert.That(property.CanRead, Is.True);
        Assert.That(property.CanWrite, Is.True);
    }

    [Test]
    public void Challenge1_BrokerOptions_HasConnectionStringProperty()
    {
        var property = typeof(BrokerOptions).GetProperty("ConnectionString");
        Assert.That(property, Is.Not.Null, "BrokerOptions must have a ConnectionString property");
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void Challenge1_BrokerOptions_HasTransactionTimeoutProperty()
    {
        var property = typeof(BrokerOptions).GetProperty("TransactionTimeoutSeconds");
        Assert.That(property, Is.Not.Null,
            "BrokerOptions must have a TransactionTimeoutSeconds property");
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Challenge1_BrokerOptions_HasSectionNameConstant()
    {
        // The SectionName constant binds to the "Broker" configuration section.
        Assert.That(BrokerOptions.SectionName, Is.EqualTo("Broker"));
    }

    [Test]
    public void Challenge1_BrokerOptions_DefaultValues_AreCorrect()
    {
        var options = new BrokerOptions();

        Assert.That(options.BrokerType, Is.EqualTo(BrokerType.NatsJetStream));
        Assert.That(options.ConnectionString, Is.EqualTo(string.Empty));
        Assert.That(options.TransactionTimeoutSeconds, Is.EqualTo(30));
    }

    // ── Challenge 2: Verify MessageHeaders Constants ────────────────────────

    [Test]
    public void Challenge2_MessageHeaders_HasExpectedTraceHeaders()
    {
        // Observability headers for distributed tracing.
        Assert.That(MessageHeaders.TraceId, Is.EqualTo("trace-id"));
        Assert.That(MessageHeaders.SpanId, Is.EqualTo("span-id"));
    }

    [Test]
    public void Challenge2_MessageHeaders_HasContentTypeHeader()
    {
        Assert.That(MessageHeaders.ContentType, Is.EqualTo("content-type"));
    }

    [Test]
    public void Challenge2_MessageHeaders_HasSourceTopicHeader()
    {
        Assert.That(MessageHeaders.SourceTopic, Is.EqualTo("source-topic"));
    }

    [Test]
    public void Challenge2_MessageHeaders_HasReplayIdHeader()
    {
        Assert.That(MessageHeaders.ReplayId, Is.EqualTo("replay-id"));
    }

    [Test]
    public void Challenge2_MessageHeaders_AllConstantsAreNonEmpty()
    {
        // Use reflection to verify every public const string is non-empty.
        var fields = typeof(MessageHeaders)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        Assert.That(fields, Is.Not.Empty, "MessageHeaders should have string constants");

        foreach (var field in fields)
        {
            var value = (string?)field.GetValue(null);
            Assert.That(value, Is.Not.Null.And.Not.Empty,
                $"MessageHeaders.{field.Name} must not be null or empty");
        }
    }

    [Test]
    public void Challenge2_MessageHeaders_ContainsAtLeast15Constants()
    {
        // Ensure the platform defines a rich set of well-known header keys.
        var constantCount = typeof(MessageHeaders)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Count(f => f.IsLiteral && f.FieldType == typeof(string));

        Assert.That(constantCount, Is.GreaterThanOrEqualTo(15));
    }
}
