using NUnit.Framework;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class MessageValidationResultTests
{
    [Test]
    public void Success_ShouldBeValid()
    {
        var result = MessageValidationResult.Success;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public void Failure_ShouldNotBeValid_AndIncludeReason()
    {
        var result = MessageValidationResult.Failure("bad payload");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("bad payload"));
    }

    [Test]
    public void Success_ShouldBeSameInstance()
    {
        var first = MessageValidationResult.Success;
        var second = MessageValidationResult.Success;
        Assert.That(first, Is.SameAs(second));
    }
}
