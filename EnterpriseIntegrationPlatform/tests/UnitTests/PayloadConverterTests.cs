using System.Text.Json;
using EnterpriseIntegrationPlatform.Processing.Transform.Converters;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class PayloadConverterTests
{
    // ================================================================== //
    // JsonToXmlConverter
    // ================================================================== //

    [Fact]
    public async Task JsonToXmlConverter_FlatObject_ProducesCorrectXml()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"orderId":"42","status":"ok"}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("<orderId>42</orderId>");
        xml.Should().Contain("<status>ok</status>");
    }

    [Fact]
    public async Task JsonToXmlConverter_NestedObject_ProducesNestedXml()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"order":{"id":1,"amount":100}}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("<order>");
        xml.Should().Contain("<id>1</id>");
        xml.Should().Contain("<amount>100</amount>");
        xml.Should().Contain("</order>");
    }

    [Fact]
    public async Task JsonToXmlConverter_ArrayValue_ProducesRepeatedElements()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"items":[{"id":1},{"id":2}]}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        var itemCount = CountOccurrences(xml, "<items>");
        itemCount.Should().Be(2);
    }

    [Fact]
    public async Task JsonToXmlConverter_BooleanTrue_ProducesTextTrue()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"active":true}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("<active>true</active>");
    }

    [Fact]
    public async Task JsonToXmlConverter_BooleanFalse_ProducesTextFalse()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"active":false}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("<active>false</active>");
    }

    [Fact]
    public async Task JsonToXmlConverter_NullValue_AddsNilAttribute()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"value":null}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("nil=\"true\"");
    }

    [Fact]
    public async Task JsonToXmlConverter_HasRootElement()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"x":1}""").RootElement;

        var xml = await sut.ConvertAsync(json);

        xml.Should().Contain("<root>");
        xml.Should().Contain("</root>");
    }

    [Fact]
    public async Task JsonToXmlConverter_CancellationRequested_ThrowsOperationCanceledException()
    {
        var sut = new JsonToXmlConverter();
        var json = JsonDocument.Parse("""{"x":1}""").RootElement;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.ConvertAsync(json, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ================================================================== //
    // XmlToJsonConverter
    // ================================================================== //

    [Fact]
    public async Task XmlToJsonConverter_FlatXml_ProducesJsonObject()
    {
        var sut = new XmlToJsonConverter();
        var xml = "<root><orderId>42</orderId><status>ok</status></root>";

        var json = await sut.ConvertAsync(xml);

        json.ValueKind.Should().Be(JsonValueKind.Object);
        json.GetProperty("orderId").GetString().Should().Be("42");
        json.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task XmlToJsonConverter_NestedXml_ProducesNestedJsonObject()
    {
        var sut = new XmlToJsonConverter();
        var xml = "<root><order><id>1</id><amount>100</amount></order></root>";

        var json = await sut.ConvertAsync(xml);

        var order = json.GetProperty("order");
        order.GetProperty("id").GetString().Should().Be("1");
        order.GetProperty("amount").GetString().Should().Be("100");
    }

    [Fact]
    public async Task XmlToJsonConverter_RepeatedElements_ProducesJsonArray()
    {
        var sut = new XmlToJsonConverter();
        var xml = "<root><item><id>1</id></item><item><id>2</id></item></root>";

        var json = await sut.ConvertAsync(xml);

        json.GetProperty("item").ValueKind.Should().Be(JsonValueKind.Array);
        json.GetProperty("item").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task XmlToJsonConverter_InvalidXml_ThrowsFormatException()
    {
        var sut = new XmlToJsonConverter();

        var act = () => sut.ConvertAsync("<not closed");

        await act.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task XmlToJsonConverter_NullInput_ThrowsArgumentNullException()
    {
        var sut = new XmlToJsonConverter();

        var act = () => sut.ConvertAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ================================================================== //
    // CsvToJsonConverter
    // ================================================================== //

    [Fact]
    public async Task CsvToJsonConverter_SimpleRows_ProducesJsonArray()
    {
        var sut = new CsvToJsonConverter();
        var csv = "id,name,status\n1,Alice,active\n2,Bob,inactive";

        var json = await sut.ConvertAsync(csv);

        json.ValueKind.Should().Be(JsonValueKind.Array);
        json.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task CsvToJsonConverter_FieldValuesCorrect()
    {
        var sut = new CsvToJsonConverter();
        var csv = "id,name\n42,Alice";

        var json = await sut.ConvertAsync(csv);

        var first = json[0];
        first.GetProperty("id").GetString().Should().Be("42");
        first.GetProperty("name").GetString().Should().Be("Alice");
    }

    [Fact]
    public async Task CsvToJsonConverter_QuotedFieldsWithComma_ParsedCorrectly()
    {
        var sut = new CsvToJsonConverter();
        var csv = "id,address\n1,\"123 Main St, Suite 4\"";

        var json = await sut.ConvertAsync(csv);

        json[0].GetProperty("address").GetString().Should().Be("123 Main St, Suite 4");
    }

    [Fact]
    public async Task CsvToJsonConverter_EscapedQuotesInField_ParsedCorrectly()
    {
        var sut = new CsvToJsonConverter();
        var csv = "id,note\n1,\"say \"\"hello\"\"\"";

        var json = await sut.ConvertAsync(csv);

        json[0].GetProperty("note").GetString().Should().Be("say \"hello\"");
    }

    [Fact]
    public async Task CsvToJsonConverter_EmptyCsv_ReturnsEmptyArray()
    {
        var sut = new CsvToJsonConverter();

        var json = await sut.ConvertAsync(string.Empty);

        json.ValueKind.Should().Be(JsonValueKind.Array);
        json.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CsvToJsonConverter_HeaderOnlyNoDataRows_ReturnsEmptyArray()
    {
        var sut = new CsvToJsonConverter();
        var csv = "id,name";

        var json = await sut.ConvertAsync(csv);

        json.ValueKind.Should().Be(JsonValueKind.Array);
        json.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CsvToJsonConverter_NullInput_ThrowsArgumentNullException()
    {
        var sut = new CsvToJsonConverter();

        var act = () => sut.ConvertAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ================================================================== //
    // Helpers
    // ================================================================== //

    private static int CountOccurrences(string source, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
