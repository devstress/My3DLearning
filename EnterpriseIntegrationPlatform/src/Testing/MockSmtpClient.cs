// ============================================================================
// MockSmtpClient – In-memory SMTP client for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Connector.Email;
using MimeKit;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="ISmtpClientWrapper"/> that
/// captures emails and tracks the connect/auth/send/disconnect lifecycle.
/// </summary>
public sealed class MockSmtpClient : ISmtpClientWrapper
{
    private readonly ConcurrentQueue<SmtpCallRecord> _calls = new();
    private readonly ConcurrentQueue<MimeMessage> _sentMessages = new();
    private bool _connected;
    private Func<string, string, Exception?>? _authFailure;

    /// <summary>All lifecycle calls recorded in order.</summary>
    public IReadOnlyList<SmtpCallRecord> Calls => _calls.ToArray();

    /// <summary>All captured MimeMessages sent through this client.</summary>
    public IReadOnlyList<MimeMessage> SentMessages => _sentMessages.ToArray();

    /// <summary>Number of send calls.</summary>
    public int SendCount => _sentMessages.Count;

    public bool IsConnected => _connected;

    /// <summary>Injects an authentication failure.</summary>
    public MockSmtpClient WithAuthFailure(Exception ex)
    {
        _authFailure = (_, _) => ex;
        return this;
    }

    /// <summary>Gets the last sent MimeMessage.</summary>
    public MimeMessage? LastSentMessage => _sentMessages.LastOrDefault();

    public Task ConnectAsync(string host, int port, bool useTls, CancellationToken ct)
    {
        _connected = true;
        _calls.Enqueue(new SmtpCallRecord("Connect", $"{host}:{port}"));
        return Task.CompletedTask;
    }

    public Task AuthenticateAsync(string username, string password, CancellationToken ct)
    {
        if (_authFailure is not null)
        {
            var ex = _authFailure(username, password);
            if (ex is not null) throw ex;
        }

        _calls.Enqueue(new SmtpCallRecord("Authenticate", username));
        return Task.CompletedTask;
    }

    public Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        _sentMessages.Enqueue(message);
        _calls.Enqueue(new SmtpCallRecord("Send", message.Subject));
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(bool quit, CancellationToken ct)
    {
        _connected = false;
        _calls.Enqueue(new SmtpCallRecord("Disconnect", quit.ToString()));
        return Task.CompletedTask;
    }

    /// <summary>Asserts the lifecycle order: Connect → Authenticate → Send → Disconnect.</summary>
    public void AssertLifecycleOrder()
    {
        var ops = _calls.Select(c => c.Operation).ToList();
        var connectIdx = ops.IndexOf("Connect");
        var authIdx = ops.IndexOf("Authenticate");
        var sendIdx = ops.IndexOf("Send");
        var disconnectIdx = ops.IndexOf("Disconnect");

        NUnit.Framework.Assert.That(connectIdx, NUnit.Framework.Is.LessThan(authIdx),
            "Connect must come before Authenticate");
        NUnit.Framework.Assert.That(authIdx, NUnit.Framework.Is.LessThan(sendIdx),
            "Authenticate must come before Send");
        NUnit.Framework.Assert.That(sendIdx, NUnit.Framework.Is.LessThan(disconnectIdx),
            "Send must come before Disconnect");
    }

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
        while (_sentMessages.TryDequeue(out _)) { }
        _connected = false;
    }

    public sealed record SmtpCallRecord(string Operation, string? Detail);
}
