using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class JsonFieldMappingTransformTests
{
    private static JsonFieldMappingTransform BuildTransform(params FieldMapping[] mappings)
    {
        var options = new TranslatorOptions { FieldMappings = [.. mappings] };
        return new JsonFieldMappingTransform(Options.Create(options));
    }

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ------------------------------------------------------------------ //
    // Basic field mapping
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_MapsSourceFieldToTargetField()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "id", TargetPath = "orderId" });
        var source = Parse("""{"id":"abc-123"}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("orderId").GetString(), Is.EqualTo("abc-123"));
    }

    [Test]
    public void Transform_MapsNestedSourcePathToFlatTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "order.id", TargetPath = "orderId" });
        var source = Parse("""{"order":{"id":"nested-id"}}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("orderId").GetString(), Is.EqualTo("nested-id"));
    }

    [Test]
    public void Transform_MapsFlatSourceToNestedTargetPath()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "city", TargetPath = "address.city" });
        var source = Parse("""{"city":"Amsterdam"}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("address").GetProperty("city").GetString(), Is.EqualTo("Amsterdam"));
    }

    // ------------------------------------------------------------------ //
    // Static value override
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_UsesStaticValue_IgnoresSourceField()
    {
        var sut = BuildTransform(
            new FieldMapping
            {
                SourcePath = "version",
                TargetPath = "schemaVersion",
                StaticValue = "2.0",
            });
        var source = Parse("""{"version":"1.0"}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("schemaVersion").GetString(), Is.EqualTo("2.0"));
    }

    // ------------------------------------------------------------------ //
    // Missing source path
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_MissingSourceField_IsOmittedFromTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "nonExistent", TargetPath = "targetField" });
        var source = Parse("""{"id":"1"}""");

        var result = sut.Transform(source);

        Assert.That(result.TryGetProperty("targetField", out _), Is.False);
    }

    // ------------------------------------------------------------------ //
    // Multiple mappings
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_MultipleFieldMappings_AllMappedToTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "id", TargetPath = "orderId" },
            new FieldMapping { SourcePath = "customer.name", TargetPath = "customerName" },
            new FieldMapping { SourcePath = "total", TargetPath = "orderTotal" });
        var source = Parse("""{"id":"O-001","customer":{"name":"Alice"},"total":"99.50"}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("orderId").GetString(), Is.EqualTo("O-001"));
        Assert.That(result.GetProperty("customerName").GetString(), Is.EqualTo("Alice"));
        Assert.That(result.GetProperty("orderTotal").GetString(), Is.EqualTo("99.50"));
    }

    // ------------------------------------------------------------------ //
    // Numeric and boolean values
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_NumericSourceValue_MappedAsString()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "amount", TargetPath = "total" });
        var source = Parse("""{"amount":42}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("total").GetString(), Is.EqualTo("42"));
    }

    [Test]
    public void Transform_BooleanSourceValue_MappedAsString()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "active", TargetPath = "isActive" });
        var source = Parse("""{"active":true}""");

        var result = sut.Transform(source);

        Assert.That(result.GetProperty("isActive").GetString(), Is.EqualTo("true"));
    }

    // ------------------------------------------------------------------ //
    // Empty mapping list
    // ------------------------------------------------------------------ //

    [Test]
    public void Transform_NoMappings_ReturnsEmptyJsonObject()
    {
        var sut = BuildTransform();
        var source = Parse("""{"id":"1"}""");

        var result = sut.Transform(source);

        Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(result.EnumerateObject(), Is.Empty);
    }
}
