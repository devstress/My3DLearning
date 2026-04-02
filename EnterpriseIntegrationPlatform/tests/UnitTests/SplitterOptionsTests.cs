using EnterpriseIntegrationPlatform.Processing.Splitter;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class SplitterOptionsTests
{
    [Test]
    public void TargetTopic_Defaults_ToEmptyString()
    {
        var options = new SplitterOptions();

        Assert.That(options.TargetTopic, Is.Empty);
    }

    [Test]
    public void TargetTopic_CanBeSet()
    {
        var options = new SplitterOptions { TargetTopic = "items.split" };

        Assert.That(options.TargetTopic, Is.EqualTo("items.split"));
    }

    [Test]
    public void TargetMessageType_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        Assert.That(options.TargetMessageType, Is.Null);
    }

    [Test]
    public void TargetMessageType_CanBeSet()
    {
        var options = new SplitterOptions { TargetMessageType = "ItemSplit" };

        Assert.That(options.TargetMessageType, Is.EqualTo("ItemSplit"));
    }

    [Test]
    public void TargetSource_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        Assert.That(options.TargetSource, Is.Null);
    }

    [Test]
    public void TargetSource_CanBeSet()
    {
        var options = new SplitterOptions { TargetSource = "Splitter" };

        Assert.That(options.TargetSource, Is.EqualTo("Splitter"));
    }

    [Test]
    public void ArrayPropertyName_Defaults_ToNull()
    {
        var options = new SplitterOptions();

        Assert.That(options.ArrayPropertyName, Is.Null);
    }

    [Test]
    public void ArrayPropertyName_CanBeSet()
    {
        var options = new SplitterOptions { ArrayPropertyName = "items" };

        Assert.That(options.ArrayPropertyName, Is.EqualTo("items"));
    }
}
