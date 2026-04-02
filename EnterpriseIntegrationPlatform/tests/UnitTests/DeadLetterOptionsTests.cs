using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class DeadLetterOptionsTests
{
    [Test]
    public void MaxRetryAttempts_Default_IsThree()
    {
        var options = new DeadLetterOptions();
        Assert.That(options.MaxRetryAttempts, Is.EqualTo(3));
    }

    [Test]
    public void MessageType_Default_IsDeadLetter()
    {
        var options = new DeadLetterOptions();
        Assert.That(options.MessageType, Is.EqualTo("DeadLetter"));
    }

    [Test]
    public void DeadLetterTopic_Default_IsEmptyString()
    {
        var options = new DeadLetterOptions();
        Assert.That(options.DeadLetterTopic, Is.Empty);
    }

    [Test]
    public void Source_Default_IsNull()
    {
        var options = new DeadLetterOptions();
        Assert.That(options.Source, Is.Null);
    }

    [Test]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new DeadLetterOptions
        {
            DeadLetterTopic = "dlq.orders",
            MaxRetryAttempts = 5,
            Source = "OrderService",
            MessageType = "CustomDeadLetter"
        };

        Assert.That(options.DeadLetterTopic, Is.EqualTo("dlq.orders"));
        Assert.That(options.MaxRetryAttempts, Is.EqualTo(5));
        Assert.That(options.Source, Is.EqualTo("OrderService"));
        Assert.That(options.MessageType, Is.EqualTo("CustomDeadLetter"));
    }
}
