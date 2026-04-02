using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class JsonArraySplitStrategyTests
{
    private static JsonArraySplitStrategy BuildStrategy(string? arrayPropertyName = null) =>
        new(Options.Create(new SplitterOptions { ArrayPropertyName = arrayPropertyName }));

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ------------------------------------------------------------------ //
    // Top-level array splitting
    // ------------------------------------------------------------------ //

    [Test]
    public void Split_TopLevelArray_ReturnsIndividualElements()
    {
        var sut = BuildStrategy();
        var payload = Parse("""[{"id":1},{"id":2},{"id":3}]""");

        var result = sut.Split(payload);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(result[1].GetProperty("id").GetInt32(), Is.EqualTo(2));
        Assert.That(result[2].GetProperty("id").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public void Split_EmptyTopLevelArray_ReturnsEmptyList()
    {
        var sut = BuildStrategy();
        var payload = Parse("[]");

        var result = sut.Split(payload);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Split_TopLevelArrayOfScalars_ReturnsScalarElements()
    {
        var sut = BuildStrategy();
        var payload = Parse("""["alpha","beta","gamma"]""");

        var result = sut.Split(payload);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].GetString(), Is.EqualTo("alpha"));
        Assert.That(result[1].GetString(), Is.EqualTo("beta"));
        Assert.That(result[2].GetString(), Is.EqualTo("gamma"));
    }

    [Test]
    public void Split_TopLevelNonArray_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy();
        var payload = Parse("""{"notAnArray":true}""");

        var act = () => sut.Split(payload);

        var ex = Assert.Throws<InvalidOperationException>(() => act());
        Assert.That(ex!.Message, Does.Contain("not a JSON array"));
    }

    // ------------------------------------------------------------------ //
    // Named array property splitting
    // ------------------------------------------------------------------ //

    [Test]
    public void Split_NamedArrayProperty_ReturnsIndividualElements()
    {
        var sut = BuildStrategy(arrayPropertyName: "items");
        var payload = Parse("""{"batchId":"B1","items":[{"id":10},{"id":20}]}""");

        var result = sut.Split(payload);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].GetProperty("id").GetInt32(), Is.EqualTo(10));
        Assert.That(result[1].GetProperty("id").GetInt32(), Is.EqualTo(20));
    }

    [Test]
    public void Split_NamedPropertyIsNotArray_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "status");
        var payload = Parse("""{"status":"active"}""");

        var act = () => sut.Split(payload);

        var ex = Assert.Throws<InvalidOperationException>(() => act());
        Assert.That(ex!.Message, Does.Contain("'status'").And.Contain("not a JSON array"));
    }

    [Test]
    public void Split_NamedPropertyNotFound_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "missing");
        var payload = Parse("""{"items":[1,2,3]}""");

        var act = () => sut.Split(payload);

        var ex = Assert.Throws<InvalidOperationException>(() => act());
        Assert.That(ex!.Message, Does.Contain("'missing'").And.Contain("not found"));
    }

    [Test]
    public void Split_NamedPropertyOnNonObject_ThrowsInvalidOperationException()
    {
        var sut = BuildStrategy(arrayPropertyName: "items");
        var payload = Parse("""[1,2,3]""");

        var act = () => sut.Split(payload);

        var ex = Assert.Throws<InvalidOperationException>(() => act());
        Assert.That(ex!.Message, Does.Contain("not a JSON object"));
    }

    // ------------------------------------------------------------------ //
    // Element independence (clone verification)
    // ------------------------------------------------------------------ //

    [Test]
    public void Split_ElementsAreIndependentClones()
    {
        var sut = BuildStrategy();
        var json = """[{"id":1,"name":"first"},{"id":2,"name":"second"}]""";
        var payload = Parse(json);

        var result = sut.Split(payload);

        // Each element should be independently serialisable (not tied to source document)
        var serialized0 = JsonSerializer.Serialize(result[0]);
        var serialized1 = JsonSerializer.Serialize(result[1]);

        Assert.That(serialized0, Does.Contain("\"id\":1"));
        Assert.That(serialized1, Does.Contain("\"id\":2"));
    }

    [Test]
    public void Split_EmptyNamedArray_ReturnsEmptyList()
    {
        var sut = BuildStrategy(arrayPropertyName: "orders");
        var payload = Parse("""{"orders":[]}""");

        var result = sut.Split(payload);

        Assert.That(result, Is.Empty);
    }
}
