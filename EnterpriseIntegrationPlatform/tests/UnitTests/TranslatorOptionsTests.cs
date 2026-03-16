using EnterpriseIntegrationPlatform.Processing.Translator;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class TranslatorOptionsTests
{
    [Fact]
    public void TranslatorOptions_Defaults_TargetTopicIsEmpty()
    {
        var options = new TranslatorOptions();

        options.TargetTopic.Should().BeEmpty();
    }

    [Fact]
    public void TranslatorOptions_Defaults_TargetMessageTypeIsNull()
    {
        var options = new TranslatorOptions();

        options.TargetMessageType.Should().BeNull();
    }

    [Fact]
    public void TranslatorOptions_Defaults_TargetSourceIsNull()
    {
        var options = new TranslatorOptions();

        options.TargetSource.Should().BeNull();
    }

    [Fact]
    public void TranslatorOptions_Defaults_FieldMappingsIsEmpty()
    {
        var options = new TranslatorOptions();

        options.FieldMappings.Should().BeEmpty();
    }

    [Fact]
    public void TranslatorOptions_CanSetTargetTopic()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };

        options.TargetTopic.Should().Be("orders.translated");
    }

    [Fact]
    public void TranslatorOptions_CanSetTargetMessageType()
    {
        var options = new TranslatorOptions { TargetMessageType = "OrderV2" };

        options.TargetMessageType.Should().Be("OrderV2");
    }

    [Fact]
    public void TranslatorOptions_CanSetTargetSource()
    {
        var options = new TranslatorOptions { TargetSource = "Translator" };

        options.TargetSource.Should().Be("Translator");
    }

    [Fact]
    public void TranslatorOptions_CanAddFieldMappings()
    {
        var mapping = new FieldMapping { SourcePath = "id", TargetPath = "orderId" };
        var options = new TranslatorOptions { FieldMappings = [mapping] };

        options.FieldMappings.Should().ContainSingle()
            .Which.SourcePath.Should().Be("id");
    }
}
