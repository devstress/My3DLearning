using EnterpriseIntegrationPlatform.Storage.Cassandra;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraDiagnosticsTests
{
    [Test]
    public void SourceName_IsCorrect()
    {
        Assert.That(CassandraDiagnostics.SourceName, Is.EqualTo("EnterpriseIntegrationPlatform.Storage.Cassandra"));
    }

    [Test]
    public void SourceVersion_IsOnePointZero()
    {
        Assert.That(CassandraDiagnostics.SourceVersion, Is.EqualTo("1.0.0"));
    }

    [Test]
    public void ActivitySource_HasCorrectName()
    {
        Assert.That(CassandraDiagnostics.ActivitySource.Name, Is.EqualTo(CassandraDiagnostics.SourceName));
    }

    [Test]
    public void ActivitySource_HasCorrectVersion()
    {
        Assert.That(CassandraDiagnostics.ActivitySource.Version, Is.EqualTo(CassandraDiagnostics.SourceVersion));
    }

    [Test]
    public void Meter_HasCorrectName()
    {
        Assert.That(CassandraDiagnostics.Meter.Name, Is.EqualTo(CassandraDiagnostics.SourceName));
    }

    [Test]
    public void Meter_HasCorrectVersion()
    {
        Assert.That(CassandraDiagnostics.Meter.Version, Is.EqualTo(CassandraDiagnostics.SourceVersion));
    }
}
