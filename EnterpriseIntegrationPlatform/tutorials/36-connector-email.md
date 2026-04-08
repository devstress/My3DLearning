# Tutorial 36 — Email Connector

Send messages as emails via SMTP with template-based body and attachment support.

## Learning Objectives

After completing this tutorial you will be able to:

1. Send email through the SMTP connector with connect → authenticate → send → disconnect lifecycle
2. Address single and multiple recipients in a single MIME message
3. Inject correlation headers into outbound email for traceability
4. Handle authentication failures while still disconnecting cleanly
5. Wire an envelope through a `MockEndpoint` into the email connector end-to-end

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Send_SingleRecipient_DelegatesToSmtp` | Single recipient delegates to SMTP |
| 2 | `Send_MultipleRecipients_SingleSmtpSend` | Multiple recipients in one send |
| 3 | `Send_NullSubject_UsesDefaultTemplate` | Null subject uses default template |
| 4 | `Send_InjectsCorrelationHeaders` | Correlation headers injected into email |
| 5 | `Send_DisconnectsEvenWhenAuthThrows` | Disconnect called even on auth failure |
| 6 | `E2E_MockEndpoint_FeedsEnvelope_ToEmailConnector` | End-to-end envelope → email connector |

> 💻 [`tests/TutorialLabs/Tutorial36/Lab.cs`](../tests/TutorialLabs/Tutorial36/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial36.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullSmtpLifecycle_ConnectAuthSendDisconnect` | 🟢 Starter | Full SMTP lifecycle (connect → auth → send → disconnect) |
| 2 | `Challenge2_MultiRecipient_MimeMessageContainsAllAddresses` | 🟡 Intermediate | Multi-recipient MIME message contains all addresses |
| 3 | `Challenge3_MockEndpoint_CustomSubjectTemplate` | 🔴 Advanced | MockEndpoint with custom subject template |

> 💻 [`tests/TutorialLabs/Tutorial36/Exam.cs`](../tests/TutorialLabs/Tutorial36/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial36.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial36.ExamAnswers"
```

---

**Previous: [← Tutorial 35 — SFTP Connector](35-connector-sftp.md)** | **Next: [Tutorial 37 — File Connector →](37-connector-file.md)**
