# Tutorial 49 — Testing Integrations

## What You'll Learn

- Testing strategies across the EIP platform test pyramid
- Unit tests with NUnit 4.4 and NSubstitute
- Contract tests for API boundary validation
- Workflow tests with Temporal local development server
- Integration tests using Testcontainers
- End-to-end browser tests with Playwright
- Load and performance benchmarks via the LoadTests project
- Testing conventions: `[SetUp]`, `Assert.That`, 1,472 unit tests

## The Test Pyramid

```
                    ┌───────────┐
                    │ Playwright │  E2E browser tests
                    │   Tests   │  (slowest, fewest)
                   ┌┴───────────┴┐
                   │  LoadTests   │  Performance benchmarks
                  ┌┴─────────────┴┐
                  │ IntegrationTests│  Testcontainers
                 ┌┴───────────────┴┐
                 │  WorkflowTests   │  Temporal local dev
                ┌┴─────────────────┴┐
                │   ContractTests    │  API contracts
               ┌┴───────────────────┴┐
               │     UnitTests        │  NUnit 4.4 + NSubstitute
               │   1,472 tests       │  (fastest, most numerous)
               └──────────────────────┘
```

## Unit Tests (NUnit 4.4 + NSubstitute)

The foundation with 1,472 tests covering all core logic:

```csharp
[TestFixture]
public class XmlNotificationMapperTests
{
    private XmlNotificationMapper _mapper;

    [SetUp]
    public void SetUp()
    {
        _mapper = new XmlNotificationMapper();
    }

    [Test]
    public void MapAck_ReturnsExpectedXml()
    {
        var envelope = CreateTestEnvelope();

        var result = _mapper.MapAck(envelope);

        Assert.That(result, Is.EqualTo("<Ack>ok</Ack>"));
    }

    [Test]
    public void MapNack_IncludesErrorMessage()
    {
        var envelope = CreateTestEnvelope();

        var result = _mapper.MapNack(envelope, "timeout");

        Assert.That(result, Does.Contain("timeout"));
    }
}
```

### Conventions

- **`[SetUp]`** for per-test initialization — fresh instances for every test
- **`Assert.That`** with constraint model (not classic `Assert.AreEqual`)
- **NSubstitute** for mocking interfaces:

```csharp
[SetUp]
public void SetUp()
{
    _featureFlags = Substitute.For<IFeatureFlagService>();
    _notificationService = Substitute.For<INatsNotificationActivityService>();
    _sut = new NotificationDecisionService(_featureFlags, _notificationService);
}

[Test]
public async Task WhenFeatureFlagDisabled_SkipsNotification()
{
    _featureFlags.IsEnabledAsync(Arg.Any<string>())
        .Returns(Task.FromResult(false));

    await _sut.HandleDeliveryResultAsync(successResult, inputWithNotifications);

    await _notificationService.DidNotReceive()
        .PublishAckAsync(Arg.Any<string>());
}
```

## Contract Tests

Validate API boundaries between services:

```csharp
[TestFixture]
public class GatewayApiContractTests
{
    [Test]
    public void IntegrationEnvelope_SerializationRoundTrip()
    {
        var original = new IntegrationEnvelope { /* ... */ };
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<IntegrationEnvelope>(json);

        Assert.That(deserialized.Source, Is.EqualTo(original.Source));
        Assert.That(deserialized.Destination, Is.EqualTo(original.Destination));
    }
}
```

## Workflow Tests (Temporal Local Dev)

Test Temporal workflows without infrastructure:

```csharp
[TestFixture]
public class IntegrationPipelineWorkflowTests
{
    private ITestWorkflowEnvironment _env;

    [SetUp]
    public async Task SetUp()
    {
        _env = await TestWorkflowEnvironment.StartLocalAsync();
    }

    [Test]
    public async Task Pipeline_CompletesAllSteps()
    {
        var worker = _env.Client.CreateWorker(/*...*/);
        var result = await _env.Client.ExecuteWorkflowAsync(
            (IntegrationPipelineWorkflow wf) => wf.RunAsync(testInput));

        Assert.That(result.Success, Is.True);
    }
}
```

## Integration Tests (Testcontainers)

Spin up real infrastructure in Docker for realistic tests:

```csharp
[TestFixture]
public class KafkaIntegrationTests
{
    private KafkaContainer _kafka;

    [SetUp]
    public async Task SetUp()
    {
        _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .Build();
        await _kafka.StartAsync();
    }

    [Test]
    public async Task PublishAndConsume_RoundTrip()
    {
        var producer = CreateProducer(_kafka.GetBootstrapAddress());
        var consumer = CreateConsumer(_kafka.GetBootstrapAddress());

        await producer.ProduceAsync("test-topic", testMessage);
        var received = consumer.Consume(TimeSpan.FromSeconds(10));

        Assert.That(received.Value, Is.EqualTo(testMessage));
    }

    [TearDown]
    public async Task TearDown() => await _kafka.DisposeAsync();
}
```

## Playwright Tests (E2E Browser)

End-to-end browser tests for the Admin dashboard:

```csharp
[TestFixture]
public class AdminDashboardTests
{
    private IPage _page;

    [Test]
    public async Task Dashboard_ShowsPipelineStatus()
    {
        await _page.GotoAsync("https://localhost:5002/dashboard");
        var status = await _page.TextContentAsync("[data-testid='pipeline-status']");

        Assert.That(status, Is.EqualTo("Healthy"));
    }
}
```

## Load Tests (Performance Benchmarks)

The `LoadTests/` project measures system throughput:

```bash
cd tests/LoadTests
dotnet run -c Release -- --duration 60 --concurrent 100 --rampUp 10
```

```
Results:
  Throughput:    2,450 msg/sec
  P50 latency:  12ms
  P95 latency:  45ms
  P99 latency:  120ms
  Error rate:   0.02%
```

## Test Organization

```
tests/
├── UnitTests/              # 1,472 fast, isolated tests
├── ContractTests/          # API serialization contracts
├── WorkflowTests/          # Temporal workflow tests
├── IntegrationTests/       # Testcontainers-based tests
├── PlaywrightTests/        # E2E browser tests
└── LoadTests/              # Performance benchmarks
```

## Scalability Dimension

The test pyramid itself scales: unit tests run in seconds on every commit,
integration tests run in CI with Testcontainers, and load tests validate that
horizontal scaling meets throughput targets before deployment.

## Atomicity Dimension

Tests verify atomicity guarantees: unit tests check that Ack/Nack is published
correctly, workflow tests validate saga compensation reverses all steps, and
integration tests confirm that DLQ routing works with real brokers.

## Exercises

1. Write a unit test for the `NotificationDecisionService` that covers all 5
   use cases (UC1–UC5). Use NSubstitute to mock `IFeatureFlagService`.

2. Create a Testcontainers integration test that verifies dead-letter queue
   routing when a consumer fails to process a message.

3. Design a load test scenario that measures the impact of enabling
   `NotificationsEnabled` on pipeline throughput. What overhead do you expect?

**Previous: [← Tutorial 48](48-notification-use-cases.md)** | **Next: [Tutorial 50 →](50-best-practices.md)**
