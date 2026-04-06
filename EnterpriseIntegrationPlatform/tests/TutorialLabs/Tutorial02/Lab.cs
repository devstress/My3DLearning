// ============================================================================
// Tutorial 02 – Environment Setup (Lab)
// ============================================================================
// EIP Pattern: Service Activator
// End-to-End: Wire real ServiceActivator and channels via DI using
// AspireIntegrationTestHost — request-reply, fire-and-forget, and
// multi-channel pipelines through actual components.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task ServiceActivator_FireAndForget_ProcessesMessageThroughDI()
    {
        // Wire real ServiceActivator via DI
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("output");
        builder.UseProducer(_output);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IServiceActivator, ServiceActivator>();
            services.Configure<ServiceActivatorOptions>(opt =>
            {
                opt.ReplySource = "OrderProcessor";
                opt.ReplyMessageType = "order.processed";
            });
        });
        _host = builder.Build();

        var activator = _host.GetService<IServiceActivator>();

        // Send a command through the real ServiceActivator (fire-and-forget)
        var command = IntegrationEnvelope<string>.Create(
            "ProcessOrder:ORD-100", "WebApp", "order.process");

        var result = await activator.InvokeAsync(command,
            (env, ct) =>
            {
                // Real service logic: log processing (fire-and-forget, no reply)
                return Task.CompletedTask;
            });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.False);
    }

    [Test]
    public async Task ServiceActivator_RequestReply_PublishesReplyToAddress()
    {
        // Wire real ServiceActivator with MockEndpoint capturing replies
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("replies");
        builder.UseProducer(_output);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IServiceActivator, ServiceActivator>();
            services.Configure<ServiceActivatorOptions>(opt =>
            {
                opt.ReplySource = "PricingService";
                opt.ReplyMessageType = "price.calculated";
            });
        });
        _host = builder.Build();

        var activator = _host.GetService<IServiceActivator>();

        // Request with ReplyTo address — ServiceActivator will publish reply
        var request = IntegrationEnvelope<string>.Create(
            "GetPrice:SKU-999", "CatalogUI", "price.request") with
        {
            ReplyTo = "price-replies",
            Intent = MessageIntent.Command,
        };

        var result = await activator.InvokeAsync<string, string>(request,
            (env, ct) =>
            {
                // Real pricing service logic
                return Task.FromResult<string?>($"Price:149.99");
            });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);
        Assert.That(result.ReplyTopic, Is.EqualTo("price-replies"));

        // Reply was published to the ReplyTo address
        _output.AssertReceivedOnTopic("price-replies", 1);
        var reply = _output.GetReceived<string>();
        Assert.That(reply.Payload, Is.EqualTo("Price:149.99"));
        Assert.That(reply.CorrelationId, Is.EqualTo(request.CorrelationId));
        Assert.That(reply.CausationId, Is.EqualTo(request.MessageId));
    }

    [Test]
    public async Task PointToPointChannel_WiredViaDI_SendsToRealBroker()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("broker");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<PointToPointChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PointToPointChannel>();

        // Wire a handler through DI-resolved channel
        IntegrationEnvelope<string>? received = null;
        await channel.ReceiveAsync<string>("task-queue", "worker",
            msg => { received = msg; return Task.CompletedTask; },
            CancellationToken.None);

        var task = IntegrationEnvelope<string>.Create(
            "ProcessReport:RPT-42", "Scheduler", "task.execute") with
        {
            Intent = MessageIntent.Command,
        };
        await channel.SendAsync(task, "task-queue", CancellationToken.None);

        // Message flowed through the DI-wired channel
        _output.AssertReceivedOnTopic("task-queue", 1);
        Assert.That(_output.GetReceived<string>().Payload, Is.EqualTo("ProcessReport:RPT-42"));

        // Handler received it
        await _output.SendAsync(task);
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Intent, Is.EqualTo(MessageIntent.Command));
    }

    [Test]
    public async Task FullPipeline_Channel_ToServiceActivator_ToReply()
    {
        // Full DI pipeline: P2P channel → ServiceActivator → reply channel
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("pipeline");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<PointToPointChannel>();
            services.AddSingleton<IServiceActivator, ServiceActivator>();
            services.Configure<ServiceActivatorOptions>(opt =>
            {
                opt.ReplySource = "InventoryService";
                opt.ReplyMessageType = "stock.checked";
            });
        });
        _host = builder.Build();

        var channel = _host.GetService<PointToPointChannel>();
        var activator = _host.GetService<IServiceActivator>();

        // Wire channel handler that invokes the service activator
        await channel.ReceiveAsync<string>("stock-checks", "inventory-checker",
            async msg =>
            {
                var request = msg with { ReplyTo = "stock-results" };
                await activator.InvokeAsync<string, string>(request,
                    (env, ct) => Task.FromResult<string?>($"InStock:{env.Payload}"));
            }, CancellationToken.None);

        // Send a stock check request through the pipeline
        var checkRequest = IntegrationEnvelope<string>.Create(
            "SKU-500", "WebStore", "stock.check") with
        {
            Intent = MessageIntent.Command,
        };
        await channel.SendAsync(checkRequest, "stock-checks", CancellationToken.None);

        // Channel published to broker
        _output.AssertReceivedOnTopic("stock-checks", 1);

        // Trigger the handler → ServiceActivator → reply
        await _output.SendAsync(checkRequest with { ReplyTo = "stock-results" });

        // ServiceActivator published the reply
        _output.AssertReceivedOnTopic("stock-results", 1);
        var reply = _output.GetReceived<string>(1);
        Assert.That(reply.Payload, Is.EqualTo("InStock:SKU-500"));
        Assert.That(reply.Source, Is.EqualTo("InventoryService"));
    }

    [Test]
    public async Task NamedEndpoints_IndependentPipelines_ThroughDI()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var ordersBroker = builder.AddMockEndpoint("orders");
        var paymentsBroker = builder.AddMockEndpoint("payments");
        _output = ordersBroker;
        _host = builder.Build();

        // Two independent pipelines using named endpoints
        var orderChannel = new PointToPointChannel(
            ordersBroker, ordersBroker,
            NullLogger<PointToPointChannel>.Instance);
        var paymentChannel = new PointToPointChannel(
            paymentsBroker, paymentsBroker,
            NullLogger<PointToPointChannel>.Instance);

        var orderMsg = IntegrationEnvelope<string>.Create(
            "NewOrder:ORD-200", "WebStore", "order.created");
        var paymentMsg = IntegrationEnvelope<string>.Create(
            "PaymentReceived:PAY-300", "PaymentGateway", "payment.received");

        await orderChannel.SendAsync(orderMsg, "orders-queue", CancellationToken.None);
        await paymentChannel.SendAsync(paymentMsg, "payments-queue", CancellationToken.None);

        // Each endpoint captured only its own messages
        ordersBroker.AssertReceivedOnTopic("orders-queue", 1);
        paymentsBroker.AssertReceivedOnTopic("payments-queue", 1);
        Assert.That(ordersBroker.GetReceived<string>().Payload, Is.EqualTo("NewOrder:ORD-200"));
        Assert.That(paymentsBroker.GetReceived<string>().Payload, Is.EqualTo("PaymentReceived:PAY-300"));
    }

    [Test]
    public async Task PublishSubscribeChannel_WiredViaDI_FanOutToMultipleHandlers()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("broker");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
            services.AddSingleton<PublishSubscribeChannel>());
        _host = builder.Build();

        var channel = _host.GetService<PublishSubscribeChannel>();

        // Two subscribers through DI-wired PubSub channel
        var auditLog = new List<string>();
        var alerts = new List<string>();

        await channel.SubscribeAsync<string>("system-events", "audit",
            msg => { auditLog.Add(msg.Payload); return Task.CompletedTask; },
            CancellationToken.None);
        await channel.SubscribeAsync<string>("system-events", "alerting",
            msg => { alerts.Add(msg.Payload); return Task.CompletedTask; },
            CancellationToken.None);

        var evt = IntegrationEnvelope<string>.Create(
            "DiskSpace:Warning:90%", "MonitoringAgent", "system.disk.warning") with
        {
            Intent = MessageIntent.Event,
            Priority = MessagePriority.High,
        };
        await channel.PublishAsync(evt, "system-events", CancellationToken.None);

        // Channel published through the DI-wired broker
        _output.AssertReceivedOnTopic("system-events", 1);

        // Fan out to both subscribers
        await _output.SendAsync(evt);
        Assert.That(auditLog, Has.Count.EqualTo(1));
        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(auditLog[0], Is.EqualTo("DiskSpace:Warning:90%"));
    }
}
