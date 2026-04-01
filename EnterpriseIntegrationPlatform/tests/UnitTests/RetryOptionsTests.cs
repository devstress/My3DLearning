using EnterpriseIntegrationPlatform.Processing.Retry;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RetryOptionsTests
{
    [Test]
    public void MaxAttempts_Default_IsThree()
    {
        var options = new RetryOptions();
        Assert.That(options.MaxAttempts, Is.EqualTo(3));
    }

    [Test]
    public void InitialDelayMs_Default_Is1000()
    {
        var options = new RetryOptions();
        Assert.That(options.InitialDelayMs, Is.EqualTo(1000));
    }

    [Test]
    public void MaxDelayMs_Default_Is30000()
    {
        var options = new RetryOptions();
        Assert.That(options.MaxDelayMs, Is.EqualTo(30000));
    }

    [Test]
    public void BackoffMultiplier_Default_IsTwo()
    {
        var options = new RetryOptions();
        Assert.That(options.BackoffMultiplier, Is.EqualTo(2.0));
    }

    [Test]
    public void UseJitter_Default_IsTrue()
    {
        var options = new RetryOptions();
        Assert.That(options.UseJitter, Is.True);
    }

    [Test]
    public void Properties_SetValues_ReturnCorrectValues()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 500,
            MaxDelayMs = 60000,
            BackoffMultiplier = 1.5,
            UseJitter = false
        };

        Assert.That(options.MaxAttempts, Is.EqualTo(5));
        Assert.That(options.InitialDelayMs, Is.EqualTo(500));
        Assert.That(options.MaxDelayMs, Is.EqualTo(60000));
        Assert.That(options.BackoffMultiplier, Is.EqualTo(1.5));
        Assert.That(options.UseJitter, Is.False);
    }
}
