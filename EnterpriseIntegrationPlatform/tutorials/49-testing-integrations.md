# Tutorial 49 — Testing Integrations

## What You'll Learn

- Testing strategies across the EIP platform test pyramid
- Unit tests with NUnit 4.4 and NSubstitute
- Contract tests for API boundary validation
- Workflow tests with Temporal local development server
- Integration tests using Testcontainers
- End-to-end browser tests with Playwright
- Load and performance benchmarks via the LoadTests project
- Testing conventions: `[SetUp]`, `Assert.That`

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
               │  (fastest, most)     │  (fastest, most numerous)
               └──────────────────────┘
```

## Unit Tests (NUnit 4.4 + NSubstitute)

The foundation of the test pyramid, covering all core logic:

```csharp
[TestFixture]
public class XmlNotificationMapperTests
{
    private XmlNotificationMapper _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new XmlNotificationMapper();
    }

    [Test]
    public void MapAck_ReturnsXmlAckOk()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var result = _sut.MapAck(messageId, correlationId);

        Assert.That(result, Is.EqualTo("<Ack>ok</Ack>"));
    }

    [Test]
    public void MapNack_IncludesErrorMessage()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var result = _sut.MapNack(messageId, correlationId, "timeout");

        Assert.That(result, Does.Contain("timeout"));
    }
}
```

### Conventions

- **`[SetUp]`** for per-test initialization — fresh instances for every test
- **`Assert.That`** with constraint model (not classic `Assert.AreEqual`)
- **NSubstitute** for mocking interfaces:

```csharp
// Example from tests/UnitTests/XmlNotificationMapperTests.cs
[TestFixture]
public class XmlNotificationMapperTests
{
    private XmlNotificationMapper _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new XmlNotificationMapper();
    }

    [Test]
    public void MapAck_ReturnsXmlAckOk()
    {
        var result = _sut.MapAck(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.EqualTo("<Ack>ok</Ack>"));
    }

    [Test]
    public void MapNack_ReturnsXmlNackWithErrorMessage()
    {
        var result = _sut.MapNack(Guid.NewGuid(), Guid.NewGuid(), "Connection timed out");

        Assert.That(result, Is.EqualTo("<Nack>not ok because of Connection timed out</Nack>"));
    }
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
    private WorkflowEnvironment? _env;

    [SetUp]
    public async Task SetUp()
    {
        try
        {
            _env = await WorkflowEnvironment.StartLocalAsync();
        }
        catch (Exception)
        {
            // Temporal local dev server not available
        }
    }

    [Test]
    public async Task Pipeline_CompletesAllSteps()
    {
        if (_env == null) return;

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
├── UnitTests/              # Fast, isolated tests (most numerous)
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

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial49/Lab.cs`](../tests/TutorialLabs/Tutorial49/Lab.cs)

**Objective:** Design a testing strategy for integration platforms, analyze the testing pyramid for **scalable** quality assurance, and evaluate infrastructure testing with Testcontainers.

### Step 1: Design the Testing Pyramid

Map the platform's testing strategy:

| Layer | Tool | Speed | Purpose | Examples |
|-------|------|-------|---------|----------|
| Unit | NUnit + NSubstitute | Fast (<1s each) | Verify individual components in isolation | Router rule evaluation, envelope immutability |
| Contract | NUnit | Medium (~5s each) | Verify interface contracts are met | `IMessageBrokerProducer` implementations |
| Integration | Testcontainers | Slow (~30s each) | Verify components work with real infrastructure | DLQ routing with real NATS, Cassandra persistence |
| Workflow | Temporal test server | Medium | Verify end-to-end workflow orchestration | Saga compensation, activity retries |
| Load | NBomber | Slow | Verify performance under sustained load | Throughput at 10K msg/s, P99 latency |

Why is this pyramid structure critical for **development scalability**? What happens if you skip the unit layer and rely only on integration?

### Step 2: Design a Testcontainers Integration Scenario

Design a DLQ routing integration scenario:

```
1. Start NATS JetStream container
2. Start Cassandra container
3. Publish a message with invalid schema
4. Verify: message appears in DLQ topic within 5 seconds
5. Verify: DLQ entry contains original envelope + error reason
6. Trigger replay via Admin API
7. Verify: message re-enters the pipeline
```

What makes this scenario un-testable with unit mocks alone? (hint: it verifies the real broker's message delivery guarantee)

### Step 3: Design a Load Profiling Scenario

Design a load scenario to measure the impact of enabling `NotificationsEnabled`:

| Metric | Without Notifications | With Notifications | Overhead |
|--------|---------------------|-------------------|----------|
| Throughput (msg/s) | ? | ? | ? |
| P50 latency | ? | ? | ? |
| P99 latency | ? | ? | ? |
| CPU usage | ? | ? | ? |

How would you use NBomber to measure this? What overhead percentage is acceptable for a notification feature?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial49/Exam.cs`](../tests/TutorialLabs/Tutorial49/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 48](48-notification-use-cases.md)** | **Next: [Tutorial 50 →](50-best-practices.md)**
