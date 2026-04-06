# Tutorial 50 — Best Practices & Design Guidelines

Apply design guidelines, avoid anti-patterns, and verify production readiness.

## Exercises

### 1. IntegrationEnvelope — IsExpired TrueForPastDate

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    "data", "Service", "event") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
};

Assert.That(envelope.IsExpired, Is.True);
```

### 2. IntegrationEnvelope — IsExpired FalseForFutureDate

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    "data", "Service", "event") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
};

Assert.That(envelope.IsExpired, Is.False);
```

### 3. InputSanitizer — Sanitize IsIdempotent

```csharp
var sanitizer = new InputSanitizer();
var input = "Hello <b>World</b>";

var first = sanitizer.Sanitize(input);
var second = sanitizer.Sanitize(first);

Assert.That(second, Is.EqualTo(first));
```

### 4. TenantResolver — NullTenantId ReturnsAnonymous

```csharp
var resolver = new TenantResolver();
var context = resolver.Resolve((string?)null);

Assert.That(context.TenantId, Is.EqualTo(TenantContext.Anonymous.TenantId));
```

### 5. MessageHeaders — ReplayId ConstantExists

```csharp
var replayId = MessageHeaders.ReplayId;

Assert.That(replayId, Is.Not.Null.And.Not.Empty);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial50/Lab.cs`](../tests/TutorialLabs/Tutorial50/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial50.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial50/Exam.cs`](../tests/TutorialLabs/Tutorial50/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial50.Exam"
```

---

**Previous: [← Tutorial 49](49-testing-integrations.md)** | **[Back to Course Overview →](README.md)**
