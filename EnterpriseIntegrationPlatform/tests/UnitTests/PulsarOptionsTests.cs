using EnterpriseIntegrationPlatform.Ingestion.Pulsar;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PulsarOptionsTests
{
    // ------------------------------------------------------------------ //
    // Defaults
    // ------------------------------------------------------------------ //

    [Test]
    public void Defaults_ServiceUrl_IsPulsarLocalhost()
    {
        var opts = new PulsarOptions();
        Assert.That(opts.ServiceUrl, Is.EqualTo("pulsar://localhost:6650"));
    }

    [Test]
    public void Defaults_OperationTimeoutMs_Is30000()
    {
        var opts = new PulsarOptions();
        Assert.That(opts.OperationTimeoutMs, Is.EqualTo(30000));
    }

    [Test]
    public void Defaults_KeepAliveIntervalMs_Is30000()
    {
        var opts = new PulsarOptions();
        Assert.That(opts.KeepAliveIntervalMs, Is.EqualTo(30000));
    }

    [Test]
    public void SectionName_IsPulsar()
    {
        Assert.That(PulsarOptions.SectionName, Is.EqualTo("Pulsar"));
    }

    // ------------------------------------------------------------------ //
    // Validate — happy path
    // ------------------------------------------------------------------ //

    [Test]
    public void Validate_Defaults_DoesNotThrow()
    {
        var opts = new PulsarOptions();
        Assert.DoesNotThrow(() => opts.Validate());
    }

    [Test]
    public void Validate_PulsarSslUrl_DoesNotThrow()
    {
        var opts = new PulsarOptions { ServiceUrl = "pulsar+ssl://broker.prod:6651" };
        Assert.DoesNotThrow(() => opts.Validate());
    }

    [Test]
    public void Validate_CustomTimeouts_DoesNotThrow()
    {
        var opts = new PulsarOptions
        {
            OperationTimeoutMs = 60000,
            KeepAliveIntervalMs = 15000,
        };
        Assert.DoesNotThrow(() => opts.Validate());
    }

    // ------------------------------------------------------------------ //
    // Validate — error cases
    // ------------------------------------------------------------------ //

    [Test]
    public void Validate_NullServiceUrl_Throws()
    {
        var opts = new PulsarOptions { ServiceUrl = null! };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_EmptyServiceUrl_Throws()
    {
        var opts = new PulsarOptions { ServiceUrl = "" };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_HttpUrl_ThrowsArgumentException()
    {
        var opts = new PulsarOptions { ServiceUrl = "http://localhost:6650" };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_InvalidUri_ThrowsArgumentException()
    {
        var opts = new PulsarOptions { ServiceUrl = "not-a-url" };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_ZeroOperationTimeoutMs_Throws()
    {
        var opts = new PulsarOptions { OperationTimeoutMs = 0 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_NegativeOperationTimeoutMs_Throws()
    {
        var opts = new PulsarOptions { OperationTimeoutMs = -1 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_ZeroKeepAliveIntervalMs_Throws()
    {
        var opts = new PulsarOptions { KeepAliveIntervalMs = 0 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_NegativeKeepAliveIntervalMs_Throws()
    {
        var opts = new PulsarOptions { KeepAliveIntervalMs = -1 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
