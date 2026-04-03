using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

[TestFixture]
public class MessageIntentTests
{
    [Test]
    public void Command_HasValueZero() =>
        Assert.That((int)MessageIntent.Command, Is.EqualTo(0));

    [Test]
    public void Document_HasValueOne() =>
        Assert.That((int)MessageIntent.Document, Is.EqualTo(1));

    [Test]
    public void Event_HasValueTwo() =>
        Assert.That((int)MessageIntent.Event, Is.EqualTo(2));

    [Test]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<MessageIntent>();

        Assert.That(values, Has.Length.EqualTo(3));
    }
}
