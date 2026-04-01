using EnterpriseIntegrationPlatform.Processing.Translator;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TranslatorOptionsTests
{
    [Test]
    public void TranslatorOptions_Defaults_TargetTopicIsEmpty()
    {
        var options = new TranslatorOptions();

        Assert.That(options.TargetTopic, Is.Empty);
    }

    [Test]
    public void TranslatorOptions_Defaults_TargetMessageTypeIsNull()
    {
        var options = new TranslatorOptions();

        Assert.That(options.TargetMessageType, Is.Null);
    }

    [Test]
    public void TranslatorOptions_Defaults_TargetSourceIsNull()
    {
        var options = new TranslatorOptions();

        Assert.That(options.TargetSource, Is.Null);
    }

    [Test]
    public void TranslatorOptions_Defaults_FieldMappingsIsEmpty()
    {
        var options = new TranslatorOptions();

        Assert.That(options.FieldMappings, Is.Empty);
    }

    [Test]
    public void TranslatorOptions_CanSetTargetTopic()
    {
        var options = new TranslatorOptions { TargetTopic = "orders.translated" };

        Assert.That(options.TargetTopic, Is.EqualTo("orders.translated"));
    }

    [Test]
    public void TranslatorOptions_CanSetTargetMessageType()
    {
        var options = new TranslatorOptions { TargetMessageType = "OrderV2" };

        Assert.That(options.TargetMessageType, Is.EqualTo("OrderV2"));
    }

    [Test]
    public void TranslatorOptions_CanSetTargetSource()
    {
        var options = new TranslatorOptions { TargetSource = "Translator" };

        Assert.That(options.TargetSource, Is.EqualTo("Translator"));
    }

    [Test]
    public void TranslatorOptions_CanAddFieldMappings()
    {
        var mapping = new FieldMapping { SourcePath = "id", TargetPath = "orderId" };
        var options = new TranslatorOptions { FieldMappings = [mapping] };

        Assert.That(options.FieldMappings, Has.Count.EqualTo(1));
        Assert.That(options.FieldMappings[0].SourcePath, Is.EqualTo("id"));
    }
}
