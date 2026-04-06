// ============================================================================
// Tutorial 02 – Environment Setup (Exam)
// ============================================================================
// EIP Pattern: Service Activator
// End-to-End: Advanced DI wiring — full channel pipelines, multiple
// endpoints, and service-activated message forwarding.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace TutorialLabs.Tutorial02;

[TestFixture]
public sealed class Exam
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
    public async Task EndToEnd_FullDIPipeline_PointToPointSendsToMock()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<PointToPointChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PointToPointChannel>();
        var envelope = IntegrationEnvelope<string>.Create(
            "DI-wired-message", "ExamService", "exam.test");

        await channel.SendAsync(envelope, "exam-queue", CancellationToken.None);

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("DI-wired-message"));
        Assert.That(received.Source, Is.EqualTo("ExamService"));
    }

    [Test]
    public async Task EndToEnd_MultipleEndpoints_IndependentMessageCapture()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var orders = builder.AddMockEndpoint("orders");
        var payments = builder.AddMockEndpoint("payments");
        _output = orders;
        _host = builder.Build();

        var orderEnv = IntegrationEnvelope<string>.Create(
            "new-order", "OrderService", "order.created");
        var paymentEnv = IntegrationEnvelope<string>.Create(
            "payment-received", "PaymentService", "payment.received");

        await orders.PublishAsync(orderEnv, "orders-topic");
        await payments.PublishAsync(paymentEnv, "payments-topic");

        orders.AssertReceivedCount(1);
        payments.AssertReceivedCount(1);
        Assert.That(orders.GetReceived<string>().Payload, Is.EqualTo("new-order"));
        Assert.That(payments.GetReceived<string>().Payload, Is.EqualTo("payment-received"));
    }

    [Test]
    public async Task EndToEnd_ServiceActivator_ProcessesAndForwards()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var input = builder.AddMockEndpoint("input");
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output).UseConsumer(input);
        builder.ConfigureServices(services =>
            services.AddSingleton<PointToPointChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PointToPointChannel>();

        // Subscribe the channel to receive on input and forward handler to output
        await channel.ReceiveAsync<string>("input-channel", "activator-group",
            async msg =>
            {
                var forwarded = msg with { Source = "Activator" };
                await _output.PublishAsync(forwarded, "output-topic");
            }, CancellationToken.None);

        // Send a message into the input endpoint
        var envelope = IntegrationEnvelope<string>.Create(
            "activate-me", "Producer", "activate");
        await input.SendAsync(envelope);

        _output.AssertReceivedCount(1);
        Assert.That(_output.GetReceived<string>().Source, Is.EqualTo("Activator"));
    }
}
