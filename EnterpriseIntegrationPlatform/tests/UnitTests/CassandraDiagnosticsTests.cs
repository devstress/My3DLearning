using EnterpriseIntegrationPlatform.Storage.Cassandra;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class CassandraDiagnosticsTests
{
    [Fact]
    public void SourceName_IsCorrect()
    {
        CassandraDiagnostics.SourceName.Should().Be("EnterpriseIntegrationPlatform.Storage.Cassandra");
    }

    [Fact]
    public void SourceVersion_IsOnePointZero()
    {
        CassandraDiagnostics.SourceVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        CassandraDiagnostics.ActivitySource.Name.Should().Be(CassandraDiagnostics.SourceName);
    }

    [Fact]
    public void ActivitySource_HasCorrectVersion()
    {
        CassandraDiagnostics.ActivitySource.Version.Should().Be(CassandraDiagnostics.SourceVersion);
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        CassandraDiagnostics.Meter.Name.Should().Be(CassandraDiagnostics.SourceName);
    }

    [Fact]
    public void Meter_HasCorrectVersion()
    {
        CassandraDiagnostics.Meter.Version.Should().Be(CassandraDiagnostics.SourceVersion);
    }
}
