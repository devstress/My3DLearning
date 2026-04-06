# Tutorial 36 — Email Connector

Send messages as emails via SMTP with template-based body and attachment support.

## Key Types

```csharp
// src/Connector.Email/IEmailConnector.cs
public interface IEmailConnector
{
    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        string toAddress,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);

    Task SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyList<string> toAddresses,
        string? subject,
        Func<T, string> bodyBuilder,
        CancellationToken ct);
}
```

```csharp
// src/Connector.Email/EmailConnectorOptions.cs
public sealed class EmailConnectorOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DefaultFrom { get; set; } = string.Empty;
    public string DefaultSubjectTemplate { get; set; } = "{MessageType} notification";
}
```

## Exercises

### 1. EmailConnectorOptions — Defaults

```csharp
var opts = new EmailConnectorOptions();

Assert.That(opts.SmtpHost, Is.EqualTo(string.Empty));
Assert.That(opts.SmtpPort, Is.EqualTo(587));
Assert.That(opts.UseTls, Is.True);
Assert.That(opts.Username, Is.EqualTo(string.Empty));
Assert.That(opts.Password, Is.EqualTo(string.Empty));
Assert.That(opts.DefaultFrom, Is.EqualTo(string.Empty));
Assert.That(opts.DefaultSubjectTemplate, Is.EqualTo("{MessageType} notification"));
```

### 2. EmailConnectorOptions — CustomValues

```csharp
var opts = new EmailConnectorOptions
{
    SmtpHost = "mail.example.com",
    SmtpPort = 465,
    UseTls = false,
    Username = "user@example.com",
    Password = "secret",
    DefaultFrom = "noreply@example.com",
    DefaultSubjectTemplate = "Alert: {MessageType}",
};

Assert.That(opts.SmtpHost, Is.EqualTo("mail.example.com"));
Assert.That(opts.SmtpPort, Is.EqualTo(465));
Assert.That(opts.UseTls, Is.False);
Assert.That(opts.Username, Is.EqualTo("user@example.com"));
Assert.That(opts.Password, Is.EqualTo("secret"));
Assert.That(opts.DefaultFrom, Is.EqualTo("noreply@example.com"));
Assert.That(opts.DefaultSubjectTemplate, Is.EqualTo("Alert: {MessageType}"));
```

### 3. ISmtpClientWrapper — InterfaceShape HasExpectedMembers

```csharp
var type = typeof(ISmtpClientWrapper);

Assert.That(type.GetMethod("ConnectAsync"), Is.Not.Null);
Assert.That(type.GetMethod("AuthenticateAsync"), Is.Not.Null);
Assert.That(type.GetMethod("SendAsync"), Is.Not.Null);
Assert.That(type.GetMethod("DisconnectAsync"), Is.Not.Null);
Assert.That(type.GetProperty("IsConnected"), Is.Not.Null);
```

### 4. EmailConnector — Send DelegatesToSmtpWrapper

```csharp
var smtpClient = Substitute.For<ISmtpClientWrapper>();
smtpClient.IsConnected.Returns(false);

var opts = Options.Create(new EmailConnectorOptions
{
    SmtpHost = "smtp.test.com",
    SmtpPort = 587,
    UseTls = true,
    Username = "user",
    Password = "pass",
    DefaultFrom = "test@test.com",
});

var connector = new EmailConnector(smtpClient, opts, NullLogger<EmailConnector>.Instance);

var envelope = IntegrationEnvelope<string>.Create("Hello", "Svc", "order.placed");

await connector.SendAsync(
    envelope, "dest@test.com", "Test Subject", p => p, CancellationToken.None);

await smtpClient.Received(1).ConnectAsync(
    Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
await smtpClient.Received(1).SendAsync(
    Arg.Any<MimeKit.MimeMessage>(), Arg.Any<CancellationToken>());
await smtpClient.Received(1).DisconnectAsync(
    Arg.Any<bool>(), Arg.Any<CancellationToken>());
```

### 5. EmailConnector — Constructor AcceptsAllDependencies

```csharp
var smtpClient = Substitute.For<ISmtpClientWrapper>();
var opts = Options.Create(new EmailConnectorOptions());
var logger = NullLogger<EmailConnector>.Instance;

var connector = new EmailConnector(smtpClient, opts, logger);

Assert.That(connector, Is.Not.Null);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial36/Lab.cs`](../tests/TutorialLabs/Tutorial36/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial36.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial36/Exam.cs`](../tests/TutorialLabs/Tutorial36/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial36.Exam"
```

---

**Previous: [← Tutorial 35 — SFTP Connector](35-connector-sftp.md)** | **Next: [Tutorial 37 — File Connector →](37-connector-file.md)**
