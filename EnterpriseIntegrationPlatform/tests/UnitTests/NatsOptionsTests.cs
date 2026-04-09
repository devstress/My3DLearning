using EnterpriseIntegrationPlatform.Ingestion.Nats;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class NatsOptionsTests
{
    [Test]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new NatsOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Url, Is.EqualTo("nats://localhost:15222"));
            Assert.That(options.MaxRetries, Is.EqualTo(3));
            Assert.That(options.RetryDelayMs, Is.EqualTo(1000));
        });
    }

    [Test]
    public void SectionName_ShouldBeNatsJetStream()
    {
        Assert.That(NatsOptions.SectionName, Is.EqualTo("NatsJetStream"));
    }

    [Test]
    public void Validate_ValidOptions_ShouldNotThrow()
    {
        var options = new NatsOptions
        {
            Url = "nats://myhost:4222",
            MaxRetries = 5,
            RetryDelayMs = 500,
        };

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_NullUrl_ShouldThrow()
    {
        var options = new NatsOptions { Url = null! };

        Assert.That(() => options.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_EmptyUrl_ShouldThrow()
    {
        var options = new NatsOptions { Url = "" };

        Assert.That(() => options.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_WhitespaceUrl_ShouldThrow()
    {
        var options = new NatsOptions { Url = "   " };

        Assert.That(() => options.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_NegativeMaxRetries_ShouldThrow()
    {
        var options = new NatsOptions { MaxRetries = -1 };

        Assert.That(
            () => options.Validate(),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("MaxRetries"));
    }

    [Test]
    public void Validate_NegativeRetryDelayMs_ShouldThrow()
    {
        var options = new NatsOptions { RetryDelayMs = -1 };

        Assert.That(
            () => options.Validate(),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("RetryDelayMs"));
    }

    [Test]
    public void Validate_ZeroMaxRetries_ShouldNotThrow()
    {
        var options = new NatsOptions { MaxRetries = 0 };

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_ZeroRetryDelayMs_ShouldNotThrow()
    {
        var options = new NatsOptions { RetryDelayMs = 0 };

        Assert.DoesNotThrow(() => options.Validate());
    }
}
