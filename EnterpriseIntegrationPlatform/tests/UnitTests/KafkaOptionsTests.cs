using EnterpriseIntegrationPlatform.Ingestion.Kafka;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class KafkaOptionsTests
{
    // ------------------------------------------------------------------ //
    // Defaults
    // ------------------------------------------------------------------ //

    [Test]
    public void Defaults_BootstrapServers_IsLocalhost9092()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void Defaults_Acks_IsAll()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.Acks, Is.EqualTo("all"));
    }

    [Test]
    public void Defaults_EnableIdempotence_IsTrue()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.EnableIdempotence, Is.True);
    }

    [Test]
    public void Defaults_CompressionType_IsNone()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.CompressionType, Is.EqualTo("none"));
    }

    [Test]
    public void Defaults_LingerMs_Is5()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.LingerMs, Is.EqualTo(5));
    }

    [Test]
    public void Defaults_BatchSize_Is16384()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.BatchSize, Is.EqualTo(16384));
    }

    [Test]
    public void Defaults_SessionTimeoutMs_Is45000()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.SessionTimeoutMs, Is.EqualTo(45000));
    }

    [Test]
    public void Defaults_EnableAutoCommit_IsFalse()
    {
        var opts = new KafkaOptions();
        Assert.That(opts.EnableAutoCommit, Is.False);
    }

    [Test]
    public void SectionName_IsKafka()
    {
        Assert.That(KafkaOptions.SectionName, Is.EqualTo("Kafka"));
    }

    // ------------------------------------------------------------------ //
    // Validate — happy path
    // ------------------------------------------------------------------ //

    [Test]
    public void Validate_Defaults_DoesNotThrow()
    {
        var opts = new KafkaOptions();
        Assert.DoesNotThrow(() => opts.Validate());
    }

    [Test]
    public void Validate_CustomValidOptions_DoesNotThrow()
    {
        var opts = new KafkaOptions
        {
            BootstrapServers = "broker1:9092,broker2:9092",
            Acks = "leader",
            LingerMs = 100,
            BatchSize = 65536,
            SessionTimeoutMs = 30000,
        };
        Assert.DoesNotThrow(() => opts.Validate());
    }

    // ------------------------------------------------------------------ //
    // Validate — error cases
    // ------------------------------------------------------------------ //

    [Test]
    public void Validate_NullBootstrapServers_Throws()
    {
        var opts = new KafkaOptions { BootstrapServers = null! };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_EmptyBootstrapServers_Throws()
    {
        var opts = new KafkaOptions { BootstrapServers = "" };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_WhitespaceBootstrapServers_Throws()
    {
        var opts = new KafkaOptions { BootstrapServers = "   " };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_NullAcks_Throws()
    {
        var opts = new KafkaOptions { Acks = null! };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Validate_NegativeLingerMs_Throws()
    {
        var opts = new KafkaOptions { LingerMs = -1 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_NegativeBatchSize_Throws()
    {
        var opts = new KafkaOptions { BatchSize = -1 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_ZeroSessionTimeoutMs_Throws()
    {
        var opts = new KafkaOptions { SessionTimeoutMs = 0 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Validate_NegativeSessionTimeoutMs_Throws()
    {
        var opts = new KafkaOptions { SessionTimeoutMs = -1 };
        Assert.That(() => opts.Validate(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
