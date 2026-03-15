using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Translator;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

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

    [Fact]
    public void Transform_MapsSourceFieldToTargetField()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "id", TargetPath = "orderId" });
        var source = Parse("""{"id":"abc-123"}""");

        var result = sut.Transform(source);

        result.GetProperty("orderId").GetString().Should().Be("abc-123");
    }

    [Fact]
    public void Transform_MapsNestedSourcePathToFlatTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "order.id", TargetPath = "orderId" });
        var source = Parse("""{"order":{"id":"nested-id"}}""");

        var result = sut.Transform(source);

        result.GetProperty("orderId").GetString().Should().Be("nested-id");
    }

    [Fact]
    public void Transform_MapsFlatSourceToNestedTargetPath()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "city", TargetPath = "address.city" });
        var source = Parse("""{"city":"Amsterdam"}""");

        var result = sut.Transform(source);

        result.GetProperty("address").GetProperty("city").GetString()
            .Should().Be("Amsterdam");
    }

    // ------------------------------------------------------------------ //
    // Static value override
    // ------------------------------------------------------------------ //

    [Fact]
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

        result.GetProperty("schemaVersion").GetString().Should().Be("2.0");
    }

    // ------------------------------------------------------------------ //
    // Missing source path
    // ------------------------------------------------------------------ //

    [Fact]
    public void Transform_MissingSourceField_IsOmittedFromTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "nonExistent", TargetPath = "targetField" });
        var source = Parse("""{"id":"1"}""");

        var result = sut.Transform(source);

        result.TryGetProperty("targetField", out _).Should().BeFalse();
    }

    // ------------------------------------------------------------------ //
    // Multiple mappings
    // ------------------------------------------------------------------ //

    [Fact]
    public void Transform_MultipleFieldMappings_AllMappedToTarget()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "id", TargetPath = "orderId" },
            new FieldMapping { SourcePath = "customer.name", TargetPath = "customerName" },
            new FieldMapping { SourcePath = "total", TargetPath = "orderTotal" });
        var source = Parse("""{"id":"O-001","customer":{"name":"Alice"},"total":"99.50"}""");

        var result = sut.Transform(source);

        result.GetProperty("orderId").GetString().Should().Be("O-001");
        result.GetProperty("customerName").GetString().Should().Be("Alice");
        result.GetProperty("orderTotal").GetString().Should().Be("99.50");
    }

    // ------------------------------------------------------------------ //
    // Numeric and boolean values
    // ------------------------------------------------------------------ //

    [Fact]
    public void Transform_NumericSourceValue_MappedAsString()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "amount", TargetPath = "total" });
        var source = Parse("""{"amount":42}""");

        var result = sut.Transform(source);

        result.GetProperty("total").GetString().Should().Be("42");
    }

    [Fact]
    public void Transform_BooleanSourceValue_MappedAsString()
    {
        var sut = BuildTransform(
            new FieldMapping { SourcePath = "active", TargetPath = "isActive" });
        var source = Parse("""{"active":true}""");

        var result = sut.Transform(source);

        result.GetProperty("isActive").GetString().Should().Be("true");
    }

    // ------------------------------------------------------------------ //
    // Empty mapping list
    // ------------------------------------------------------------------ //

    [Fact]
    public void Transform_NoMappings_ReturnsEmptyJsonObject()
    {
        var sut = BuildTransform();
        var source = Parse("""{"id":"1"}""");

        var result = sut.Transform(source);

        result.ValueKind.Should().Be(JsonValueKind.Object);
        result.EnumerateObject().Should().BeEmpty();
    }
}
