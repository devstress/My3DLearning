// ============================================================================
// Tutorial 02 – Environment Setup (Lab)
// ============================================================================
// EIP Pattern: Service Activator
// End-to-End: Build AspireIntegrationTestHost, register & resolve services,
// verify DI wiring with MockEndpoints.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Lab
{
    private AspireIntegrationTestHost _host = null!;
    private MockEndpoint _output = null!;

    [TearDown]
    public async Task TearDown()
    {
        if (_host is not null) await _host.DisposeAsync();
        if (_output is not null) await _output.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_HostResolvesProducer_PublishCapturedByMock()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output);
        _host = builder.Build();

        var producer = _host.GetService<IMessageBrokerProducer>();
        var envelope = IntegrationEnvelope<string>.Create("hello", "lab", "test");

        await producer.PublishAsync(envelope, "topic");

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("hello"));
    }

    [Test]
    public async Task EndToEnd_HostResolvesConsumer_SubscribeReceivesMessage()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("input");
        builder.UseConsumer(_output);
        _host = builder.Build();

        var consumer = _host.GetService<IMessageBrokerConsumer>();
        IntegrationEnvelope<string>? received = null;
        await consumer.SubscribeAsync<string>("topic", "group", msg =>
        {
            received = msg;
            return Task.CompletedTask;
        });

        var envelope = IntegrationEnvelope<string>.Create("data", "lab", "test");
        await _output.SendAsync(envelope);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Payload, Is.EqualTo("data"));
    }

    [Test]
    public async Task EndToEnd_NamedEndpoints_RetrievedByName()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var ep1 = builder.AddMockEndpoint("orders");
        var ep2 = builder.AddMockEndpoint("payments");
        builder.UseProducer(ep1);
        _host = builder.Build();

        var envelope = IntegrationEnvelope<string>.Create("order-1", "lab", "test");
        await _host.GetEndpoint("orders").PublishAsync(envelope, "topic");

        _host.GetEndpoint("orders").AssertReceivedCount(1);
        _host.GetEndpoint("payments").AssertNoneReceived();
        _output = ep1;
    }

    [Test]
    public async Task EndToEnd_CustomServiceRegistration_ResolvedFromHost()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<IGreetingService, GreetingService>());
        _host = builder.Build();

        var service = _host.GetService<IGreetingService>();
        var envelope = IntegrationEnvelope<string>.Create(
            service.Greet("World"), "lab", "greeting");

        var producer = _host.GetService<IMessageBrokerProducer>();
        await producer.PublishAsync(envelope, "greetings");

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public async Task EndToEnd_PointToPointChannel_WiredThroughDI()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<PointToPointChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PointToPointChannel>();
        var envelope = IntegrationEnvelope<string>.Create("p2p", "lab", "test");

        await channel.SendAsync(envelope, "queue", CancellationToken.None);

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("p2p"));
    }

    [Test]
    public async Task EndToEnd_PublishSubscribeChannel_WiredThroughDI()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<PublishSubscribeChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PublishSubscribeChannel>();
        var envelope = IntegrationEnvelope<string>.Create("pubsub", "lab", "test");

        await channel.PublishAsync(envelope, "fanout", CancellationToken.None);

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("pubsub"));
    }
}

public interface IGreetingService { string Greet(string name); }
public class GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
