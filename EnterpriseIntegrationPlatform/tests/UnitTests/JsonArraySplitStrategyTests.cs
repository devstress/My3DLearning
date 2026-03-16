using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class JsonArraySplitStrategyTests
{
    private static JsonArraySplitStrategy BuildStrategy(string? arrayPropertyName = null) =>
        new(Options.Create(new SplitterOptions { ArrayPropertyName = arrayPropertyName }));

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ------------------------------------------------------------------ //
    // Top-level array splitting
    // ------------------------------------------------------------------ //

    [Fact]
    public void Split_TopLevelArray_ReturnsIndividualElements()
    {
        var sut = BuildStrategy();
        var payload = Parse("""[{"id":1},{"id":2},{"id":3}]""");

        var result = sut.Split(payload);

        result.Should().HaveCount(3);
        result[0].GetProperty("id").GetInt32().Should().Be(1);
        result[1].GetProperty("id").GetInt32().Should().Be(2);
        result[2].GetProperty("id").GetInt32().Should().Be(3);
    }

    [Fact]
    public void Split_EmptyTopLevelArray_ReturnsEmptyList()
    {
        var sut = BuildStrategy();
        var payload = Parse("[]");

        var result = sut.Split(payload);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Split_TopLevelArrayOfScalars_ReturnsScalarElements()
    {
        var sut = BuildStrategy();
        var payload = Parse("""["alpha","beta","gamma"]""");

        var result = sut.Split(payload);

        result.Should().HaveCount(3);
        result[0].GetString().Should().Be("alpha");
        result[1].GetString().Should().Be("beta");
        result[2].GetString().Should().Be("gamma");
    }

    [Fact]
    public void Split_TopLevelNonArray_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy();
        var payload = Parse("""{"notAnArray":true}""");

        var act = () => sut.Split(payload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a JSON array*");
    }

    // ------------------------------------------------------------------ //
    // Named array property splitting
    // ------------------------------------------------------------------ //

    [Fact]
    public void Split_NamedArrayProperty_ReturnsIndividualElements()
    {
        var sut = BuildStrategy(arrayPropertyName: "items");
        var payload = Parse("""{"batchId":"B1","items":[{"id":10},{"id":20}]}""");

        var result = sut.Split(payload);

        result.Should().HaveCount(2);
        result[0].GetProperty("id").GetInt32().Should().Be(10);
        result[1].GetProperty("id").GetInt32().Should().Be(20);
    }

    [Fact]
    public void Split_NamedPropertyIsNotArray_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "status");
        var payload = Parse("""{"status":"active"}""");

        var act = () => sut.Split(payload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'status'*not a JSON array*");
    }

    [Fact]
    public void Split_NamedPropertyNotFound_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "missing");
        var payload = Parse("""{"items":[1,2,3]}""");

        var act = () => sut.Split(payload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'missing'*not found*");
    }

    [Fact]
    public void Split_NamedPropertyOnNonObject_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "items");
        var payload = Parse("""[1,2,3]""");

        var act = () => sut.Split(payload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a JSON object*");
    }

    // ------------------------------------------------------------------ //
    // Element independence (clone verification)
    // ------------------------------------------------------------------ //

    [Fact]
    public void Split_ElementsAreIndependentClones()
    {
        var sut = BuildStrategy();
        var json = """[{"id":1,"name":"first"},{"id":2,"name":"second"}]""";
        var payload = Parse(json);

        var result = sut.Split(payload);

        // Each element should be independently serialisable (not tied to source document)
        var serialized0 = JsonSerializer.Serialize(result[0]);
        var serialized1 = JsonSerializer.Serialize(result[1]);

        serialized0.Should().Contain("\"id\":1");
        serialized1.Should().Contain("\"id\":2");
    }

    [Fact]
    public void Split_EmptyNamedArray_ReturnsEmptyList()
    {
        var sut = BuildStrategy(arrayPropertyName: "orders");
        var payload = Parse("""{"orders":[]}""");

        var result = sut.Split(payload);

        result.Should().BeEmpty();
    }
}
