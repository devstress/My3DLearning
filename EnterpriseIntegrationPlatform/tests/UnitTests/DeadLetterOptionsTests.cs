using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class DeadLetterOptionsTests
{
    [Fact]
    public void MaxRetryAttempts_Default_IsThree()
    {
        var options = new DeadLetterOptions();
        options.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void MessageType_Default_IsDeadLetter()
    {
        var options = new DeadLetterOptions();
        options.MessageType.Should().Be("DeadLetter");
    }

    [Fact]
    public void DeadLetterTopic_Default_IsEmptyString()
    {
        var options = new DeadLetterOptions();
        options.DeadLetterTopic.Should().BeEmpty();
    }

    [Fact]
    public void Source_Default_IsNull()
    {
        var options = new DeadLetterOptions();
        options.Source.Should().BeNull();
    }

    [Fact]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new DeadLetterOptions
        {
            DeadLetterTopic = "dlq.orders",
            MaxRetryAttempts = 5,
            Source = "OrderService",
            MessageType = "CustomDeadLetter"
        };

        options.DeadLetterTopic.Should().Be("dlq.orders");
        options.MaxRetryAttempts.Should().Be(5);
        options.Source.Should().Be("OrderService");
        options.MessageType.Should().Be("CustomDeadLetter");
    }
}
