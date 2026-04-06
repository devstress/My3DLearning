// ============================================================================
// MockActivityServices – In-memory activity services for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="ICompensationActivityService"/>
/// that returns configurable results per step name.
/// </summary>
public sealed class MockCompensationActivityService : ICompensationActivityService
{
    private readonly Dictionary<string, bool> _stepResults = new();
    private readonly ConcurrentQueue<CompensationCallRecord> _calls = new();
    private bool _defaultResult = true;

    /// <summary>All compensation calls recorded.</summary>
    public IReadOnlyList<CompensationCallRecord> Calls => _calls.ToArray();

    /// <summary>Sets the result for a specific step name.</summary>
    public MockCompensationActivityService WithStepResult(string stepName, bool success)
    {
        _stepResults[stepName] = success;
        return this;
    }

    /// <summary>Sets the default result for unmatched steps.</summary>
    public MockCompensationActivityService WithDefaultResult(bool success)
    {
        _defaultResult = success;
        return this;
    }

    public Task<bool> CompensateAsync(Guid correlationId, string stepName)
    {
        _calls.Enqueue(new CompensationCallRecord(correlationId, stepName));
        var result = _stepResults.TryGetValue(stepName, out var r) ? r : _defaultResult;
        return Task.FromResult(result);
    }

    public sealed record CompensationCallRecord(Guid CorrelationId, string StepName);
}

/// <summary>
/// Real in-memory implementation of <see cref="IMessageValidationService"/>
/// that returns configurable validation results per message type.
/// </summary>
public sealed class MockMessageValidationService : IMessageValidationService
{
    private readonly Dictionary<string, MessageValidationResult> _results = new();
    private readonly ConcurrentQueue<ValidationCallRecord> _calls = new();
    private MessageValidationResult _defaultResult = MessageValidationResult.Success;

    /// <summary>All validation calls recorded.</summary>
    public IReadOnlyList<ValidationCallRecord> Calls => _calls.ToArray();

    /// <summary>Sets the result for a specific message type.</summary>
    public MockMessageValidationService WithResult(string messageType, MessageValidationResult result)
    {
        _results[messageType] = result;
        return this;
    }

    /// <summary>Sets the default result for unmatched types.</summary>
    public MockMessageValidationService WithDefaultResult(MessageValidationResult result)
    {
        _defaultResult = result;
        return this;
    }

    public Task<MessageValidationResult> ValidateAsync(string messageType, string payloadJson)
    {
        _calls.Enqueue(new ValidationCallRecord(messageType, payloadJson));
        var result = _results.TryGetValue(messageType, out var r) ? r : _defaultResult;
        return Task.FromResult(result);
    }

    public sealed record ValidationCallRecord(string MessageType, string PayloadJson);
}

/// <summary>
/// Real in-memory implementation of <see cref="IPersistenceActivityService"/>
/// that captures all persistence calls.
/// </summary>
public sealed class MockPersistenceActivityService : IPersistenceActivityService
{
    private readonly ConcurrentQueue<PersistenceCallRecord> _calls = new();

    /// <summary>All persistence calls recorded.</summary>
    public IReadOnlyList<PersistenceCallRecord> Calls => _calls.ToArray();

    /// <summary>Number of SaveMessage calls.</summary>
    public int SaveCount => _calls.Count(c => c.Operation == "SaveMessage");

    /// <summary>Number of UpdateDeliveryStatus calls.</summary>
    public int UpdateStatusCount => _calls.Count(c => c.Operation == "UpdateDeliveryStatus");

    /// <summary>Number of SaveFault calls.</summary>
    public int SaveFaultCount => _calls.Count(c => c.Operation == "SaveFault");

    public Task SaveMessageAsync(IntegrationPipelineInput input, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new PersistenceCallRecord("SaveMessage", input.MessageId, input.MessageType, null));
        return Task.CompletedTask;
    }

    public Task UpdateDeliveryStatusAsync(
        Guid messageId, Guid correlationId, DateTimeOffset recordedAt,
        string status, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new PersistenceCallRecord("UpdateDeliveryStatus", messageId, status, null));
        return Task.CompletedTask;
    }

    public Task SaveFaultAsync(
        Guid messageId, Guid correlationId, string messageType,
        string faultedBy, string reason, int retryCount,
        CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new PersistenceCallRecord("SaveFault", messageId, messageType, reason));
        return Task.CompletedTask;
    }

    /// <summary>Asserts that SaveMessage was called the expected number of times.</summary>
    public void AssertSaveCount(int expected) =>
        NUnit.Framework.Assert.That(SaveCount, NUnit.Framework.Is.EqualTo(expected));

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record PersistenceCallRecord(string Operation, Guid MessageId, string? Detail, string? Reason);
}

/// <summary>
/// Real in-memory implementation of <see cref="IMessageLoggingService"/>
/// that captures all log entries.
/// </summary>
public sealed class MockMessageLoggingService : IMessageLoggingService
{
    private readonly ConcurrentQueue<LogRecord> _logs = new();

    /// <summary>All log entries recorded.</summary>
    public IReadOnlyList<LogRecord> Logs => _logs.ToArray();

    /// <summary>Number of log entries.</summary>
    public int LogCount => _logs.Count;

    public Task LogAsync(Guid messageId, string messageType, string stage)
    {
        _logs.Enqueue(new LogRecord(messageId, messageType, stage));
        return Task.CompletedTask;
    }

    /// <summary>Asserts a specific stage was logged for the given message.</summary>
    public void AssertLogged(Guid messageId, string stage) =>
        NUnit.Framework.Assert.That(
            _logs.Any(l => l.MessageId == messageId && l.Stage == stage),
            NUnit.Framework.Is.True,
            $"Expected log entry for message {messageId} at stage '{stage}'");

    public void Reset()
    {
        while (_logs.TryDequeue(out _)) { }
    }

    public sealed record LogRecord(Guid MessageId, string MessageType, string Stage);
}
