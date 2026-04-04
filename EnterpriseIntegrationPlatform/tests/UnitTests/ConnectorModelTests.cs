using EnterpriseIntegrationPlatform.Connectors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ConnectorResultTests
{
    [Test]
    public void Ok_WithConnectorName_SetSuccessTrueAndConnectorName()
    {
        var result = ConnectorResult.Ok("http-api");

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("http-api"));
        Assert.That(result.StatusMessage, Is.Null);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Ok_WithStatusMessage_SetsMessage()
    {
        var result = ConnectorResult.Ok("sftp-upload", "Uploaded 3 files");

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("sftp-upload"));
        Assert.That(result.StatusMessage, Is.EqualTo("Uploaded 3 files"));
    }

    [Test]
    public void Fail_WithErrorMessage_SetSuccessFalseAndErrorMessage()
    {
        var result = ConnectorResult.Fail("email-sender", "SMTP timeout");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ConnectorName, Is.EqualTo("email-sender"));
        Assert.That(result.ErrorMessage, Is.EqualTo("SMTP timeout"));
    }

    [Test]
    public void CompletedAt_Default_IsApproximatelyUtcNow()
    {
        var before = DateTimeOffset.UtcNow;

        var result = ConnectorResult.Ok("timing-test");

        var after = DateTimeOffset.UtcNow;
        Assert.That(result.CompletedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(result.CompletedAt, Is.LessThanOrEqualTo(after));
    }
}

[TestFixture]
public class ConnectorSendOptionsTests
{
    [Test]
    public void Destination_Default_IsNull()
    {
        var options = new ConnectorSendOptions();

        Assert.That(options.Destination, Is.Null);
    }

    [Test]
    public void Properties_Default_IsEmptyDictionary()
    {
        var options = new ConnectorSendOptions();

        Assert.That(options.Properties, Is.Not.Null);
        Assert.That(options.Properties, Is.Empty);
    }

    [Test]
    public void Properties_WithPopulatedDictionary_CanBeRead()
    {
        var props = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Correlation-Id"] = "abc-123",
        };

        var options = new ConnectorSendOptions { Properties = props };

        Assert.That(options.Properties, Has.Count.EqualTo(2));
        Assert.That(options.Properties["Content-Type"], Is.EqualTo("application/json"));
        Assert.That(options.Properties["X-Correlation-Id"], Is.EqualTo("abc-123"));
    }
}

[TestFixture]
public class ConnectorDescriptorTests
{
    [Test]
    public void Enabled_Default_IsTrue()
    {
        var descriptor = new ConnectorDescriptor
        {
            Name = "test",
            ConnectorType = ConnectorType.Http,
            ImplementationType = typeof(object),
        };

        Assert.That(descriptor.Enabled, Is.True);
    }

    [Test]
    public void AllProperties_SetExplicitly_ReturnExpectedValues()
    {
        var descriptor = new ConnectorDescriptor
        {
            Name = "my-sftp",
            ConnectorType = ConnectorType.Sftp,
            Enabled = false,
            Description = "Vendor SFTP connector",
            ImplementationType = typeof(string),
        };

        Assert.That(descriptor.Name, Is.EqualTo("my-sftp"));
        Assert.That(descriptor.ConnectorType, Is.EqualTo(ConnectorType.Sftp));
        Assert.That(descriptor.Enabled, Is.False);
        Assert.That(descriptor.Description, Is.EqualTo("Vendor SFTP connector"));
        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(string)));
    }
}

[TestFixture]
public class ConnectorTypeEnumTests
{
    [Test]
    public void ConnectorType_HasAllFourValues()
    {
        var values = Enum.GetValues<ConnectorType>();

        Assert.That(values, Has.Length.EqualTo(4));
        Assert.That(values, Does.Contain(ConnectorType.Http));
        Assert.That(values, Does.Contain(ConnectorType.Sftp));
        Assert.That(values, Does.Contain(ConnectorType.Email));
        Assert.That(values, Does.Contain(ConnectorType.File));
    }
}

[TestFixture]
public class ConnectorRegistryEdgeCaseTests
{
    [Test]
    public void Register_WhitespaceName_ThrowsArgumentException()
    {
        var registry = new ConnectorRegistry();
        var connector = Substitute.For<IConnector>();
        connector.Name.Returns("   ");

        Assert.Throws<ArgumentException>(() => registry.Register(connector));
    }

    [Test]
    public void Remove_NullName_ThrowsArgumentException()
    {
        var registry = new ConnectorRegistry();

        // ThrowIfNullOrWhiteSpace throws ArgumentNullException (subclass of ArgumentException) for null
        Assert.Throws<ArgumentNullException>(() => registry.Remove(null!));
    }
}

[TestFixture]
public class ConnectorFactoryEdgeCaseTests
{
    [Test]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<ConnectorFactory>>();

        Assert.Throws<ArgumentNullException>(() => new ConnectorFactory(null!, logger));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IConnectorRegistry>();

        Assert.Throws<ArgumentNullException>(() => new ConnectorFactory(registry, null!));
    }
}
