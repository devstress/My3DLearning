// ============================================================================
// Tutorial 02 – Environment Setup (Lab)
// ============================================================================
// This lab verifies that your development environment is correctly configured
// by using reflection to confirm that all key platform types, enums, and
// namespaces are available and correctly structured.
// ============================================================================

using System.Reflection;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using NUnit.Framework;

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Lab
{
    // ── Verify Core Types Exist ─────────────────────────────────────────────

    [Test]
    public void IntegrationEnvelope_TypeExists()
    {
        var type = typeof(IntegrationEnvelope<string>);
        Assert.That(type, Is.Not.Null);
        Assert.That(type.IsGenericType || type.IsClass, Is.True);
    }

    [Test]
    public void IMessageBrokerProducer_InterfaceExists()
    {
        var type = typeof(IMessageBrokerProducer);
        Assert.That(type.IsInterface, Is.True);
    }

    [Test]
    public void IMessageBrokerConsumer_InterfaceExists()
    {
        var type = typeof(IMessageBrokerConsumer);
        Assert.That(type.IsInterface, Is.True);

        // Consumer also implements IAsyncDisposable for resource cleanup.
        Assert.That(typeof(IAsyncDisposable).IsAssignableFrom(type), Is.True);
    }

    [Test]
    public void BrokerOptions_ClassExists()
    {
        var type = typeof(BrokerOptions);
        Assert.That(type, Is.Not.Null);
        Assert.That(type.IsClass, Is.True);
        Assert.That(type.IsSealed, Is.True);
    }

    // ── Verify BrokerType Enum ──────────────────────────────────────────────

    [Test]
    public void BrokerType_HasNatsJetStreamValue()
    {
        Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.NatsJetStream), Is.True);
    }

    [Test]
    public void BrokerType_HasKafkaValue()
    {
        Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.Kafka), Is.True);
    }

    [Test]
    public void BrokerType_HasPulsarValue()
    {
        Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.Pulsar), Is.True);
    }

    [Test]
    public void BrokerType_HasExactlyThreeValues()
    {
        var values = Enum.GetValues<BrokerType>();
        Assert.That(values, Has.Length.EqualTo(3));
    }

    // ── Verify MessagePriority Enum ─────────────────────────────────────────

    [Test]
    [TestCase(MessagePriority.Low, 0)]
    [TestCase(MessagePriority.Normal, 1)]
    [TestCase(MessagePriority.High, 2)]
    [TestCase(MessagePriority.Critical, 3)]
    public void MessagePriority_HasExpectedValues(MessagePriority priority, int expected)
    {
        Assert.That((int)priority, Is.EqualTo(expected));
    }

    [Test]
    public void MessagePriority_HasExactlyFourValues()
    {
        var values = Enum.GetValues<MessagePriority>();
        Assert.That(values, Has.Length.EqualTo(4));
    }

    // ── Verify MessageIntent Enum ───────────────────────────────────────────

    [Test]
    [TestCase(MessageIntent.Command, 0)]
    [TestCase(MessageIntent.Document, 1)]
    [TestCase(MessageIntent.Event, 2)]
    public void MessageIntent_HasExpectedValues(MessageIntent intent, int expected)
    {
        Assert.That((int)intent, Is.EqualTo(expected));
    }

    // ── Verify Namespace Presence via Assembly ──────────────────────────────

    [Test]
    public void ContractsNamespace_ContainsExpectedTypes()
    {
        var assembly = typeof(IntegrationEnvelope<>).Assembly;
        var typeNames = assembly.GetTypes()
            .Where(t => t.Namespace == "EnterpriseIntegrationPlatform.Contracts")
            .Select(t => t.Name)
            .ToList();

        Assert.That(typeNames, Does.Contain("MessagePriority"));
        Assert.That(typeNames, Does.Contain("MessageIntent"));
        Assert.That(typeNames, Does.Contain("MessageHeaders"));
    }

    [Test]
    public void IngestionNamespace_ContainsExpectedTypes()
    {
        var assembly = typeof(IMessageBrokerProducer).Assembly;
        var typeNames = assembly.GetTypes()
            .Where(t => t.Namespace == "EnterpriseIntegrationPlatform.Ingestion")
            .Select(t => t.Name)
            .ToList();

        Assert.That(typeNames, Does.Contain("IMessageBrokerProducer"));
        Assert.That(typeNames, Does.Contain("IMessageBrokerConsumer"));
        Assert.That(typeNames, Does.Contain("BrokerOptions"));
        Assert.That(typeNames, Does.Contain("BrokerType"));
    }
}
