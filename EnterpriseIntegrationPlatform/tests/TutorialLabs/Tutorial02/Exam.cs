// ============================================================================
// Tutorial 02 – Environment Setup (Exam)
// ============================================================================
// EIP Pattern: Service Activator + Message Channel Pipeline
// End-to-End: Advanced DI wiring — full multi-stage pipelines with real
// ServiceActivator, PointToPointChannel, PublishSubscribeChannel, and
// request-reply orchestration through actual components.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using EnterpriseIntegrationPlatform.Processing.Dispatcher;
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
    public async Task MultiStage_ChannelToActivatorToChannel_FullPipeline()
    {
        // Full DI pipeline: input P2P → ServiceActivator → output PubSub
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("pipeline");
        builder.UseProducer(_output).UseConsumer(_output);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<PointToPointChannel>();
            services.AddSingleton<PublishSubscribeChannel>();
            services.AddSingleton<IServiceActivator, ServiceActivator>();
            services.Configure<ServiceActivatorOptions>(opt =>
            {
                opt.ReplySource = "EnrichmentService";
                opt.ReplyMessageType = "data.enriched";
            });
        });
        _host = builder.Build();

        var inputChannel = _host.GetService<PointToPointChannel>();
        var outputChannel = _host.GetService<PublishSubscribeChannel>();
        var activator = _host.GetService<IServiceActivator>();

        // Wire pipeline: P2P receive → activator → PubSub publish
        await inputChannel.ReceiveAsync<string>("raw-data", "enrichment-worker",
            async msg =>
            {
                // ServiceActivator processes the message
                await activator.InvokeAsync(msg, (env, ct) =>
                {
                    // Enrich and forward to output channel
                    var enriched = env with
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            ["enriched-by"] = "EnrichmentService",
                            ["original-source"] = env.Source,
                        },
                    };
                    return outputChannel.PublishAsync(enriched, "enriched-data", ct);
                });
            }, CancellationToken.None);

        // Send raw data into the pipeline
        var rawData = IntegrationEnvelope<string>.Create(
            "customer:CUST-100", "DataIngestion", "data.raw") with
        {
            Intent = MessageIntent.Document,
        };
        await inputChannel.SendAsync(rawData, "raw-data", CancellationToken.None);

        // Input channel published to broker
        _output.AssertReceivedOnTopic("raw-data", 1);

        // Trigger the pipeline
        await _output.SendAsync(rawData);

        // Output channel published the enriched data
        _output.AssertReceivedOnTopic("enriched-data", 1);
        var enriched = _output.GetReceived<string>(1);
        Assert.That(enriched.Payload, Is.EqualTo("customer:CUST-100"));
        Assert.That(enriched.Metadata["enriched-by"], Is.EqualTo("EnrichmentService"));
        Assert.That(enriched.Metadata["original-source"], Is.EqualTo("DataIngestion"));
    }

    [Test]
    public async Task RequestReply_ThroughDIPipeline_CausationChainPreserved()
    {
        var builder = AspireIntegrationTestHost.CreateBuilder();
        _output = builder.AddMockEndpoint("broker");
        builder.UseProducer(_output);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IServiceActivator, ServiceActivator>();
            services.Configure<ServiceActivatorOptions>(opt =>
            {
                opt.ReplySource = "ValidationService";
                opt.ReplyMessageType = "validation.result";
            });
        });
        _host = builder.Build();

        var activator = _host.GetService<IServiceActivator>();

        // Request-reply: validate an order and return result
        var request = IntegrationEnvelope<string>.Create(
            "ValidateOrder:ORD-888", "CheckoutService", "order.validate") with
        {
            Intent = MessageIntent.Command,
            ReplyTo = "validation-replies",
        };

        var result = await activator.InvokeAsync<string, string>(request,
            (env, ct) =>
            {
                // Real validation logic
                var orderId = env.Payload.Split(':')[1];
                return Task.FromResult<string?>($"Valid:{orderId}");
            });

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ReplySent, Is.True);

        // Reply arrived at the ReplyTo address
        _output.AssertReceivedOnTopic("validation-replies", 1);
        var reply = _output.GetReceived<string>();
        Assert.That(reply.Payload, Is.EqualTo("Valid:ORD-888"));
        Assert.That(reply.Source, Is.EqualTo("ValidationService"));
        Assert.That(reply.CorrelationId, Is.EqualTo(request.CorrelationId));
        Assert.That(reply.CausationId, Is.EqualTo(request.MessageId));
    }

    [Test]
    public async Task MultipleEndpoints_IndependentActivators_ProcessInParallel()
    {
        // Two independent ServiceActivator pipelines with separate endpoints
        var builder = AspireIntegrationTestHost.CreateBuilder();
        var orderEndpoint = builder.AddMockEndpoint("orders");
        var inventoryEndpoint = builder.AddMockEndpoint("inventory");
        _output = orderEndpoint;
        _host = builder.Build();

        // Each endpoint gets its own ServiceActivator
        var orderActivator = new ServiceActivator(
            orderEndpoint,
            Microsoft.Extensions.Options.Options.Create(new ServiceActivatorOptions
            {
                ReplySource = "OrderService",
                ReplyMessageType = "order.confirmed",
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceActivator>.Instance);

        var inventoryActivator = new ServiceActivator(
            inventoryEndpoint,
            Microsoft.Extensions.Options.Options.Create(new ServiceActivatorOptions
            {
                ReplySource = "InventoryService",
                ReplyMessageType = "stock.reserved",
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceActivator>.Instance);

        // Process order confirmation
        var orderRequest = IntegrationEnvelope<string>.Create(
            "ConfirmOrder:ORD-500", "Checkout", "order.confirm") with
        {
            ReplyTo = "order-confirmations",
            Intent = MessageIntent.Command,
        };

        // Process inventory reservation
        var inventoryRequest = IntegrationEnvelope<string>.Create(
            "ReserveStock:SKU-200:5", "Checkout", "stock.reserve") with
        {
            ReplyTo = "stock-reservations",
            Intent = MessageIntent.Command,
        };

        // Both activators process independently
        var orderResult = await orderActivator.InvokeAsync<string, string>(orderRequest,
            (env, ct) => Task.FromResult<string?>($"Confirmed:{env.Payload.Split(':')[1]}"));
        var inventoryResult = await inventoryActivator.InvokeAsync<string, string>(inventoryRequest,
            (env, ct) => Task.FromResult<string?>($"Reserved:{env.Payload.Split(':')[1]}:5units"));

        // Each endpoint captured only its own replies
        Assert.That(orderResult.Succeeded, Is.True);
        Assert.That(inventoryResult.Succeeded, Is.True);
        orderEndpoint.AssertReceivedOnTopic("order-confirmations", 1);
        inventoryEndpoint.AssertReceivedOnTopic("stock-reservations", 1);

        Assert.That(orderEndpoint.GetReceived<string>().Payload, Is.EqualTo("Confirmed:ORD-500"));
        Assert.That(inventoryEndpoint.GetReceived<string>().Payload, Is.EqualTo("Reserved:SKU-200:5units"));
    }
}
