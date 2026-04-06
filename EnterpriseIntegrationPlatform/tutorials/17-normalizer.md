# Tutorial 17 — Normalizer

Auto-detect JSON, XML, and CSV payloads and convert them to canonical JSON using `INormalizer`.

---

## Key Types

```csharp
// src/Processing.Transform/INormalizer.cs
public interface INormalizer
{
    Task<NormalizationResult> NormalizeAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/NormalizationResult.cs
public sealed record NormalizationResult(
    string Payload,
    string OriginalContentType,
    string DetectedFormat,       // "JSON", "XML", "CSV"
    bool WasTransformed);
```

```csharp
// src/Processing.Transform/NormalizerOptions.cs
public sealed class NormalizerOptions
{
    public bool StrictContentType { get; init; } = true;
    public char CsvDelimiter { get; init; } = ',';
    public bool CsvHasHeaders { get; init; } = true;
    public string XmlRootName { get; init; } = "Root";
}
```

---

## Exercises

### Exercise 1: JSON payload passes through unchanged

```csharp
var options = Options.Create(new NormalizerOptions());
var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

var json = """{"name":"Alice","age":30}""";

var result = await normalizer.NormalizeAsync(json, "application/json");

Assert.That(result.DetectedFormat, Is.EqualTo("JSON"));
Assert.That(result.WasTransformed, Is.False);
Assert.That(result.OriginalContentType, Is.EqualTo("application/json"));

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
```

### Exercise 2: XML payload converts to JSON

```csharp
var options = Options.Create(new NormalizerOptions());
var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

var xml = "<Order><id>ORD-1</id><total>99.50</total></Order>";

var result = await normalizer.NormalizeAsync(xml, "application/xml");

Assert.That(result.DetectedFormat, Is.EqualTo("XML"));
Assert.That(result.WasTransformed, Is.True);

using var doc = JsonDocument.Parse(result.Payload);
Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo("ORD-1"));
Assert.That(doc.RootElement.GetProperty("total").GetString(), Is.EqualTo("99.50"));
```

### Exercise 3: CSV payload converts to JSON array

```csharp
var options = Options.Create(new NormalizerOptions());
var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

var csv = "name,age\nAlice,30\nBob,25";

var result = await normalizer.NormalizeAsync(csv, "text/csv");

Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
Assert.That(result.WasTransformed, Is.True);

using var doc = JsonDocument.Parse(result.Payload);
var array = doc.RootElement.GetProperty("Root");
Assert.That(array.GetArrayLength(), Is.EqualTo(2));
Assert.That(array[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
Assert.That(array[1].GetProperty("name").GetString(), Is.EqualTo("Bob"));
```

### Exercise 4: Unknown content type in strict mode throws

```csharp
var options = Options.Create(new NormalizerOptions { StrictContentType = true });
var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

Assert.ThrowsAsync<InvalidOperationException>(
    () => normalizer.NormalizeAsync("{}", "application/octet-stream"));
```

### Exercise 5: Custom CSV delimiter parses correctly

```csharp
var options = Options.Create(new NormalizerOptions { CsvDelimiter = ';' });
var normalizer = new MessageNormalizer(options, NullLogger<MessageNormalizer>.Instance);

var csv = "name;age\nAlice;30";

var result = await normalizer.NormalizeAsync(csv, "text/csv");

Assert.That(result.DetectedFormat, Is.EqualTo("CSV"));
Assert.That(result.WasTransformed, Is.True);

using var doc = JsonDocument.Parse(result.Payload);
var array = doc.RootElement.GetProperty("Root");
Assert.That(array[0].GetProperty("name").GetString(), Is.EqualTo("Alice"));
Assert.That(array[0].GetProperty("age").GetString(), Is.EqualTo("30"));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial17/Lab.cs`](../tests/TutorialLabs/Tutorial17/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial17.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial17/Exam.cs`](../tests/TutorialLabs/Tutorial17/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial17.Exam"
```

---

**Previous: [← Tutorial 16 — Transform Pipeline](16-transform-pipeline.md)** | **Next: [Tutorial 18 — Content Enricher →](18-content-enricher.md)**
