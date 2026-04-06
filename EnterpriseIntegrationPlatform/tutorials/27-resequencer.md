# Tutorial 27 — Resequencer

Reorder out-of-sequence messages back into their original sequence.

## Key Types

```csharp
// src/Processing.Resequencer/IResequencer.cs
public interface IResequencer
{
    IReadOnlyList<IntegrationEnvelope<T>> Accept<T>(IntegrationEnvelope<T> envelope);
    IReadOnlyList<IntegrationEnvelope<T>> ReleaseOnTimeout<T>(Guid correlationId);
    int ActiveSequenceCount { get; }
}
```

```csharp
// src/Processing.Resequencer/ResequencerOptions.cs
public sealed class ResequencerOptions
{
    public TimeSpan ReleaseTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentSequences { get; set; } = 10_000;
}
```

## Exercises

### 1. Accept — CompleteSequenceInOrder ReleasesAllMessages

```csharp
var resequencer = CreateResequencer();
var correlationId = Guid.NewGuid();

var r1 = resequencer.Accept(MakeSequenced(correlationId, 0, 3));
var r2 = resequencer.Accept(MakeSequenced(correlationId, 1, 3));
var r3 = resequencer.Accept(MakeSequenced(correlationId, 2, 3));

// Only the last accept should release all 3
Assert.That(r1, Is.Empty);
Assert.That(r2, Is.Empty);
Assert.That(r3, Has.Count.EqualTo(3));
Assert.That(r3[0].Payload, Is.EqualTo("msg-0"));
Assert.That(r3[1].Payload, Is.EqualTo("msg-1"));
Assert.That(r3[2].Payload, Is.EqualTo("msg-2"));
```

### 2. Accept — OutOfOrder ReleasesInCorrectOrder

```csharp
var resequencer = CreateResequencer();
var correlationId = Guid.NewGuid();

var r1 = resequencer.Accept(MakeSequenced(correlationId, 2, 3));
var r2 = resequencer.Accept(MakeSequenced(correlationId, 0, 3));
var r3 = resequencer.Accept(MakeSequenced(correlationId, 1, 3));

Assert.That(r1, Is.Empty);
Assert.That(r2, Is.Empty);
Assert.That(r3, Has.Count.EqualTo(3));
Assert.That(r3[0].Payload, Is.EqualTo("msg-0"));
Assert.That(r3[1].Payload, Is.EqualTo("msg-1"));
Assert.That(r3[2].Payload, Is.EqualTo("msg-2"));
```

### 3. Accept — IncompleteSequence BuffersAndReturnsEmpty

```csharp
var resequencer = CreateResequencer();
var correlationId = Guid.NewGuid();

var result = resequencer.Accept(MakeSequenced(correlationId, 1, 3));

Assert.That(result, Is.Empty);
Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));
```

### 4. Accept — DuplicateSequenceNumber IsIgnored

```csharp
var resequencer = CreateResequencer();
var correlationId = Guid.NewGuid();

resequencer.Accept(MakeSequenced(correlationId, 0, 2));
var dup = resequencer.Accept(MakeSequenced(correlationId, 0, 2));

Assert.That(dup, Is.Empty);
// Still waiting for seq 1
Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(1));
```

### 5. ReleaseOnTimeout — IncompleteSequence ReturnsBufferedInOrder

```csharp
var resequencer = CreateResequencer();
var correlationId = Guid.NewGuid();

resequencer.Accept(MakeSequenced(correlationId, 2, 5));
resequencer.Accept(MakeSequenced(correlationId, 0, 5));

var released = resequencer.ReleaseOnTimeout<string>(correlationId);

Assert.That(released, Has.Count.EqualTo(2));
Assert.That(released[0].Payload, Is.EqualTo("msg-0"));
Assert.That(released[1].Payload, Is.EqualTo("msg-2"));
Assert.That(resequencer.ActiveSequenceCount, Is.EqualTo(0));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial27/Lab.cs`](../tests/TutorialLabs/Tutorial27/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial27.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial27/Exam.cs`](../tests/TutorialLabs/Tutorial27/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial27.Exam"
```

---

**Previous: [← Tutorial 26 — Message Replay](26-message-replay.md)** | **Next: [Tutorial 28 — Competing Consumers →](28-competing-consumers.md)**
