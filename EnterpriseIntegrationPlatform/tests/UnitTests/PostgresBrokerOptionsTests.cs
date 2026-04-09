using EnterpriseIntegrationPlatform.Ingestion.Postgres;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PostgresBrokerOptionsTests
{
    [Test]
    public void SectionName_IsBrokerPostgres()
    {
        Assert.That(PostgresBrokerOptions.SectionName, Is.EqualTo("Broker:Postgres"));
    }

    [Test]
    public void Defaults_ConnectionString_IsEmpty()
    {
        var options = new PostgresBrokerOptions();

        Assert.That(options.ConnectionString, Is.Empty);
    }

    [Test]
    public void Defaults_PollIntervalMs_Is1000()
    {
        var options = new PostgresBrokerOptions();

        Assert.That(options.PollIntervalMs, Is.EqualTo(1000));
    }

    [Test]
    public void Defaults_PollBatchSize_Is100()
    {
        var options = new PostgresBrokerOptions();

        Assert.That(options.PollBatchSize, Is.EqualTo(100));
    }

    [Test]
    public void Defaults_LockTimeoutSeconds_Is30()
    {
        var options = new PostgresBrokerOptions();

        Assert.That(options.LockTimeoutSeconds, Is.EqualTo(30));
    }

    [Test]
    public void Defaults_RetentionHours_Is24()
    {
        var options = new PostgresBrokerOptions();

        Assert.That(options.RetentionHours, Is.EqualTo(24));
    }

    [Test]
    public void AllDefaults_AreIndependentlySettable()
    {
        var options = new PostgresBrokerOptions
        {
            ConnectionString = "Host=myhost;Database=mydb;Username=u;Password=p",
            PollIntervalMs = 500,
            PollBatchSize = 50,
            LockTimeoutSeconds = 60,
            RetentionHours = 48,
        };

        Assert.That(options.ConnectionString, Is.EqualTo("Host=myhost;Database=mydb;Username=u;Password=p"));
        Assert.That(options.PollIntervalMs, Is.EqualTo(500));
        Assert.That(options.PollBatchSize, Is.EqualTo(50));
        Assert.That(options.LockTimeoutSeconds, Is.EqualTo(60));
        Assert.That(options.RetentionHours, Is.EqualTo(48));
    }
}
