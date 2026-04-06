# Tutorial 02 — Setting Up Your Environment

Wire real EIP components via dependency injection using `AspireIntegrationTestHost`. This tutorial demonstrates the Service Activator pattern — connecting messaging infrastructure to application services with request-reply and fire-and-forget processing.

## Key Types

```csharp
// src/Processing.Dispatcher/ServiceActivator.cs — connects messaging to services
public sealed class ServiceActivator : IServiceActivator
{
    // Invokes a service operation from a message, publishes reply to ReplyTo address
    Task<ServiceActivatorResult> InvokeAsync<TRequest, TResponse>(
        IntegrationEnvelope<TRequest> envelope,
        Func<IntegrationEnvelope<TRequest>, CancellationToken, Task<TResponse?>> serviceOperation,
        CancellationToken cancellationToken = default);

    // Fire-and-forget: invoke service with no reply
    Task<ServiceActivatorResult> InvokeAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<IntegrationEnvelope<T>, CancellationToken, Task> serviceOperation,
        CancellationToken cancellationToken = default);
}

// src/Processing.Dispatcher/ServiceActivatorOptions.cs
public sealed class ServiceActivatorOptions
{
    public string ReplySource { get; set; } = "ServiceActivator";
    public string ReplyMessageType { get; set; } = "service-activator.reply";
}

// src/Testing/AspireIntegrationTestHost.cs — DI host for integration wiring
public sealed class AspireIntegrationTestHost : IAsyncDisposable
{
    public static Builder CreateBuilder();
    public T GetService<T>() where T : notnull;
    public MockEndpoint GetEndpoint(string name);
}
```

## Exercises

### 1. Wire ServiceActivator via DI for fire-and-forget

```csharp
var builder = AspireIntegrationTestHost.CreateBuilder();
var output = builder.AddMockEndpoint("output");
builder.UseProducer(output);
builder.ConfigureServices(services =>
{
    services.AddSingleton<IServiceActivator, ServiceActivator>();
    services.Configure<ServiceActivatorOptions>(opt =>
    {
        opt.ReplySource = "OrderProcessor";
        opt.ReplyMessageType = "order.processed";
    });
});
var host = builder.Build();

var activator = host.GetService<IServiceActivator>();
var command = IntegrationEnvelope<string>.Create("ProcessOrder:ORD-100", "WebApp", "order.process");

var result = await activator.InvokeAsync(command,
    (env, ct) => Task.CompletedTask); // Fire-and-forget

// result.Succeeded == true, result.ReplySent == false
```

### 2. ServiceActivator request-reply with ReplyTo

```csharp
var request = IntegrationEnvelope<string>.Create(
    "GetPrice:SKU-999", "CatalogUI", "price.request") with
{
    ReplyTo = "price-replies",
    Intent = MessageIntent.Command,
};

var result = await activator.InvokeAsync<string, string>(request,
    (env, ct) => Task.FromResult<string?>("Price:149.99"));

// result.ReplySent == true — reply published to "price-replies"
// Causation chain: reply.CausationId == request.MessageId
```

### 3. Full pipeline: P2P channel → ServiceActivator → reply

```csharp
await channel.ReceiveAsync<string>("stock-checks", "inventory-checker",
    async msg =>
    {
        var request = msg with { ReplyTo = "stock-results" };
        await activator.InvokeAsync<string, string>(request,
            (env, ct) => Task.FromResult<string?>($"InStock:{env.Payload}"));
    }, CancellationToken.None);
```

### 4. Multiple named endpoints for independent pipelines

```csharp
var ordersBroker = builder.AddMockEndpoint("orders");
var paymentsBroker = builder.AddMockEndpoint("payments");
// Each endpoint routes through its own channel — fully independent
```

### 5. PubSub channel with multiple handlers wired through DI

```csharp
var channel = host.GetService<PublishSubscribeChannel>();
await channel.SubscribeAsync<string>("system-events", "audit",
    msg => { /* audit */ return Task.CompletedTask; }, CancellationToken.None);
await channel.SubscribeAsync<string>("system-events", "alerting",
    msg => { /* alert */ return Task.CompletedTask; }, CancellationToken.None);
// Both subscribers receive every message
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial02/Lab.cs`](../tests/TutorialLabs/Tutorial02/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial02/Exam.cs`](../tests/TutorialLabs/Tutorial02/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Exam"
```

---

**Previous: [← Tutorial 01 — Introduction](01-introduction.md)** | **Next: [Tutorial 03 — Your First Message →](03-first-message.md)**
