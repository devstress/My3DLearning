# Tutorial 33 — Security

Sanitize input, encrypt payloads, and validate messages against security threats.

## Key Types

```csharp
// src/Security/IInputSanitizer.cs
public interface IInputSanitizer
{
    string Sanitize(string input);
    bool IsClean(string input);
}
```

```csharp
// src/Security/IPayloadSizeGuard.cs
public interface IPayloadSizeGuard
{
    void Enforce(string payload);
    void Enforce(byte[] payloadBytes);
}
```

```csharp
// src/Security/PayloadTooLargeException.cs
public sealed class PayloadTooLargeException : Exception
{
    public int ActualBytes { get; }
    public int MaxBytes { get; }
}
```

```csharp
// src/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public bool ValidateLifetime { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
```

## Exercises

### 1. InputSanitizer — Sanitize RemovesScriptTags

```csharp
var input = "Hello <script>alert('xss')</script> World";

var result = _sanitizer.Sanitize(input);

Assert.That(result, Does.Not.Contain("<script>"));
Assert.That(result, Does.Not.Contain("alert"));
Assert.That(result, Does.Contain("Hello"));
Assert.That(result, Does.Contain("World"));
```

### 2. InputSanitizer — IsClean ReturnsFalseForXss

```csharp
var dirty = "<script>alert('xss')</script>";

Assert.That(_sanitizer.IsClean(dirty), Is.False);
```

### 3. InputSanitizer — IsClean ReturnsTrueForClean

```csharp
var clean = "Hello, this is perfectly safe text.";

Assert.That(_sanitizer.IsClean(clean), Is.True);
```

### 4. PayloadSizeGuard — Enforce PassesForSmallPayload

```csharp
var guard = new PayloadSizeGuard(
    Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 1024 }));

var smallPayload = new string('x', 100);

Assert.DoesNotThrow(() => guard.Enforce(smallPayload));
```

### 5. PayloadSizeGuard — Enforce ThrowsPayloadTooLargeException

```csharp
var guard = new PayloadSizeGuard(
    Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 50 }));

var oversized = new string('x', 200);

var ex = Assert.Throws<PayloadTooLargeException>(
    () => guard.Enforce(oversized));

Assert.That(ex!.MaxBytes, Is.EqualTo(50));
Assert.That(ex.ActualBytes, Is.GreaterThan(50));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial33/Lab.cs`](../tests/TutorialLabs/Tutorial33/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial33.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial33/Exam.cs`](../tests/TutorialLabs/Tutorial33/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial33.Exam"
```

---

**Previous: [← Tutorial 32 — Multi-Tenancy](32-multi-tenancy.md)** | **Next: [Tutorial 34 — HTTP Connector →](34-connector-http.md)**
