// ============================================================================
// MockObservabilityServices – In-memory observability services for testing
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Observability;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="IObservabilityEventLog"/>
/// that stores events in memory and supports queries.
/// </summary>
public sealed class MockObservabilityEventLog : IObservabilityEventLog
{
    private readonly ConcurrentBag<MessageEvent> _events = new();

    /// <summary>All recorded events.</summary>
    public IReadOnlyList<MessageEvent> Events => _events.ToList();

    /// <summary>Pre-populates the event log with events.</summary>
    public MockObservabilityEventLog WithEvents(params MessageEvent[] events)
    {
        foreach (var e in events) _events.Add(e);
        return this;
    }

    public Task RecordAsync(MessageEvent messageEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(messageEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MessageEvent>> GetByBusinessKeyAsync(
        string businessKey, CancellationToken cancellationToken = default)
    {
        var result = _events
            .Where(e => e.BusinessKey == businessKey)
            .OrderBy(e => e.RecordedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<MessageEvent>>(result);
    }

    public Task<IReadOnlyList<MessageEvent>> GetByCorrelationIdAsync(
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var result = _events
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.RecordedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<MessageEvent>>(result);
    }

    public void Reset() => _events.Clear();
}

/// <summary>
/// Real in-memory implementation of <see cref="ITraceAnalyzer"/> that returns
/// configurable analysis responses.
/// </summary>
public sealed class MockTraceAnalyzer : ITraceAnalyzer
{
    private readonly ConcurrentQueue<TraceCallRecord> _calls = new();
    private string _analyseResponse = "Mock trace analysis";
    private string _whereIsResponse = "Message is being processed";

    /// <summary>All calls recorded.</summary>
    public IReadOnlyList<TraceCallRecord> Calls => _calls.ToArray();

    /// <summary>Sets the response for AnalyseTraceAsync.</summary>
    public MockTraceAnalyzer WithAnalyseResponse(string response)
    {
        _analyseResponse = response;
        return this;
    }

    /// <summary>Sets the response for WhereIsMessageAsync.</summary>
    public MockTraceAnalyzer WithWhereIsResponse(string response)
    {
        _whereIsResponse = response;
        return this;
    }

    public Task<string> AnalyseTraceAsync(string traceContextJson, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new TraceCallRecord("AnalyseTrace", traceContextJson));
        return Task.FromResult(_analyseResponse);
    }

    public Task<string> WhereIsMessageAsync(Guid correlationId, string knownState, CancellationToken cancellationToken = default)
    {
        _calls.Enqueue(new TraceCallRecord("WhereIsMessage", $"{correlationId}:{knownState}"));
        return Task.FromResult(_whereIsResponse);
    }

    public void Reset()
    {
        while (_calls.TryDequeue(out _)) { }
    }

    public sealed record TraceCallRecord(string Operation, string Detail);
}
