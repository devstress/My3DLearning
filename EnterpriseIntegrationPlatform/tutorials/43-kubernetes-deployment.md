# Tutorial 43 — Kubernetes Deployment

Deploy the platform to Kubernetes with Helm charts and Kustomize overlays.

## Exercises

### 1. TemporalOptions — PropertiesAssignable

```csharp
var opts = new TemporalOptions
{
    ServerAddress = "temporal.prod:7233",
    Namespace = "production",
    TaskQueue = "order-workflows",
};

Assert.That(opts.ServerAddress, Is.EqualTo("temporal.prod:7233"));
Assert.That(opts.Namespace, Is.EqualTo("production"));
Assert.That(opts.TaskQueue, Is.EqualTo("order-workflows"));
```

### 2. PipelineOptions — PropertiesAssignable

```csharp
var opts = new PipelineOptions
{
    AckSubject = "pipeline.ack",
    NackSubject = "pipeline.nack",
    InboundSubject = "pipeline.inbound",
    NatsUrl = "nats://nats-server:4222",
    ConsumerGroup = "my-group",
};

Assert.That(opts.AckSubject, Is.EqualTo("pipeline.ack"));
Assert.That(opts.NackSubject, Is.EqualTo("pipeline.nack"));
Assert.That(opts.InboundSubject, Is.EqualTo("pipeline.inbound"));
Assert.That(opts.NatsUrl, Is.EqualTo("nats://nats-server:4222"));
Assert.That(opts.ConsumerGroup, Is.EqualTo("my-group"));
```

### 3. JwtOptions — Defaults ValidateLifetimeAndClockSkew

```csharp
var opts = new JwtOptions();

Assert.That(opts.ValidateLifetime, Is.True);
Assert.That(opts.ClockSkew, Is.EqualTo(TimeSpan.FromMinutes(5)));
Assert.That(opts.Issuer, Is.EqualTo(string.Empty));
Assert.That(opts.Audience, Is.EqualTo(string.Empty));
Assert.That(opts.SigningKey, Is.EqualTo(string.Empty));
```

### 4. DisasterRecoveryOptions — Defaults

```csharp
var opts = new DisasterRecoveryOptions();

Assert.That(opts.MaxDrillHistorySize, Is.EqualTo(100));
Assert.That(opts.MaxReplicationLag, Is.EqualTo(TimeSpan.FromSeconds(30)));
Assert.That(opts.HealthCheckInterval, Is.EqualTo(TimeSpan.FromSeconds(10)));
Assert.That(opts.OfflineThreshold, Is.EqualTo(3));
Assert.That(opts.PerItemReplicationTime, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
```

### 5. OptionsCreate — TemporalOptions WorksCorrectly

```csharp
var temporal = new TemporalOptions
{
    ServerAddress = "localhost:7233",
    Namespace = "test-ns",
    TaskQueue = "test-queue",
};

var wrapped = Options.Create(temporal);

Assert.That(wrapped, Is.Not.Null);
Assert.That(wrapped.Value.ServerAddress, Is.EqualTo("localhost:7233"));
Assert.That(wrapped.Value.Namespace, Is.EqualTo("test-ns"));
Assert.That(wrapped.Value.TaskQueue, Is.EqualTo("test-queue"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial43/Lab.cs`](../tests/TutorialLabs/Tutorial43/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial43.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial43/Exam.cs`](../tests/TutorialLabs/Tutorial43/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial43.Exam"
```

---

**Previous: [← Tutorial 42](42-configuration.md)** | **Next: [Tutorial 44 →](44-disaster-recovery.md)**
