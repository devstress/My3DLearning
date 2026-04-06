// ============================================================================
// Tutorial 05 – Message Brokers (Exam)
// ============================================================================
// EIP Pattern: Message Endpoint
// End-to-End: Multi-broker fan-out, selective consumer filtering, and full
// AspireIntegrationTestHost pipeline with broker configuration.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;

namespace TutorialLabs.Tutorial05;

[TestFixture]
public sealed class Exam
{
    private MockEndpoint _nats = null!;
    private MockEndpoint _kafka = null!;
    private MockEndpoint _pulsar = null!;

    [SetUp]
    public void SetUp()
    {
        _nats = new MockEndpoint("nats");
        _kafka = new MockEndpoint("kafka");
        _pulsar = new MockEndpoint("pulsar");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _nats.DisposeAsync();
        await _kafka.DisposeAsync();
        await _pulsar.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_MultiBrokerFanOut_AllEndpointsReceive()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "critical-event", "AlertService", "alert.raised") with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Event,
        };

        await _nats.PublishAsync(envelope, "alerts");
        await _kafka.PublishAsync(envelope, "alerts");
        await _pulsar.PublishAsync(envelope, "alerts");

        _nats.AssertReceivedCount(1);
        _kafka.AssertReceivedCount(1);
        _pulsar.AssertReceivedCount(1);

        Assert.That(_nats.GetReceived<string>().Payload, Is.EqualTo("critical-event"));
        Assert.That(_kafka.GetReceived<string>().Payload, Is.EqualTo("critical-event"));
        Assert.That(_pulsar.GetReceived<string>().Payload, Is.EqualTo("critical-event"));
    }

    [Test]
    public async Task EndToEnd_SelectiveConsumer_FiltersMessages()
    {
        var results = new List<string>();
        await _nats.SubscribeAsync<string>("orders", "group",
            env => env.Priority == MessagePriority.High,
            msg =>
            {
                results.Add(msg.Payload);
                return Task.CompletedTask;
            });

        var highPriority = IntegrationEnvelope<string>.Create(
            "urgent-order", "svc", "order") with { Priority = MessagePriority.High };
        var lowPriority = IntegrationEnvelope<string>.Create(
            "normal-order", "svc", "order") with { Priority = MessagePriority.Low };

        await _nats.SendAsync(highPriority);
        await _nats.SendAsync(lowPriority);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.EqualTo("urgent-order"));
    }

    [Test]
    public async Task EndToEnd_FullPipeline_HostWithBrokerConfig()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var output = builder.AddMockEndpoint("output");
        builder.UseProducer(output);
        builder.Configure<BrokerOptions>(opts =>
        {
            opts.BrokerType = BrokerType.NatsJetStream;
            opts.ConnectionString = "nats://localhost:15222";
        });
        await using var host = builder.Build();

        var producer = host.GetService<IMessageBrokerProducer>();
        var envelope = IntegrationEnvelope<string>.Create(
            "host-message", "HostService", "host.event");

        await producer.PublishAsync(envelope, "host-topic");

        output.AssertReceivedCount(1);
        Assert.That(output.GetReceived<string>().Payload, Is.EqualTo("host-message"));
    }
}
