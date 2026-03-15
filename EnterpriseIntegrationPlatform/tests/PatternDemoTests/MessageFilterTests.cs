using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Message Filter pattern.
/// Discards messages that do not match criteria before processing.
/// BizTalk equivalent: Receive Port filter expressions, subscription filters.
/// EIP: Message Filter (p. 237)
/// </summary>
public class MessageFilterTests
{
    private record SensorReading(string SensorId, double Temperature);

    [Fact]
    public void Accepts_Message_WhenAllPredicatesMatch()
    {
        var filter = new MessageFilter<SensorReading>()
            .MustSatisfy(e => e.Payload.Temperature > 0)
            .MustSatisfy(e => e.Payload.SensorId.StartsWith("PROD"));

        var envelope = IntegrationEnvelope<SensorReading>.Create(
            new SensorReading("PROD-42", 25.5), "IoT", "SensorReading");

        filter.Accept(envelope).Should().BeTrue();
    }

    [Fact]
    public void Rejects_Message_WhenPredicateFails()
    {
        var filter = new MessageFilter<SensorReading>()
            .MustSatisfy(e => e.Payload.Temperature > 0)
            .MustSatisfy(e => e.Payload.SensorId.StartsWith("PROD"));

        var envelope = IntegrationEnvelope<SensorReading>.Create(
            new SensorReading("TEST-01", 25.5), "IoT", "SensorReading");

        filter.Accept(envelope).Should().BeFalse();
    }

    [Fact]
    public void Accepts_All_WhenNoPredicates()
    {
        var filter = new MessageFilter<SensorReading>();

        var envelope = IntegrationEnvelope<SensorReading>.Create(
            new SensorReading("ANY", -10), "IoT", "SensorReading");

        filter.Accept(envelope).Should().BeTrue();
    }
}
