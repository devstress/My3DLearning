// ============================================================================
// MockTemporalWorkflowDispatcher – Real in-memory pipeline executor
// ============================================================================
// Executes the integration pipeline steps (persist → validate → ack/nack) in
// memory, providing the same all-or-nothing semantics as the real Temporal
// workflow. Captures all dispatches for test assertions.
// ============================================================================

using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Demo.Pipeline;

namespace EnterpriseIntegrationPlatform.Testing;

/// <summary>
/// Real in-memory implementation of <see cref="ITemporalWorkflowDispatcher"/>
/// that executes pipeline logic without requiring a Temporal server.
/// Captures all dispatched inputs and workflow IDs for test assertions.
/// Supports configurable validation, persistence, and failure injection.
/// </summary>
public sealed class MockTemporalWorkflowDispatcher : ITemporalWorkflowDispatcher
{
    private readonly ConcurrentQueue<DispatchRecord> _dispatches = new();
    private Func<IntegrationPipelineInput, string, IntegrationPipelineResult>? _handler;

    /// <summary>All dispatches recorded by this mock.</summary>
    public IReadOnlyList<DispatchRecord> Dispatches => _dispatches.ToArray();

    /// <summary>Number of dispatches recorded.</summary>
    public int DispatchCount => _dispatches.Count;

    /// <summary>
    /// Configures a custom handler that processes each dispatch.
    /// If not set, the dispatcher returns a success result by default.
    /// </summary>
    public MockTemporalWorkflowDispatcher OnDispatch(
        Func<IntegrationPipelineInput, string, IntegrationPipelineResult> handler)
    {
        _handler = handler;
        return this;
    }

    /// <summary>Configures the dispatcher to always return success.</summary>
    public MockTemporalWorkflowDispatcher ReturnsSuccess()
    {
        _handler = (input, _) => new IntegrationPipelineResult(input.MessageId, true);
        return this;
    }

    /// <summary>Configures the dispatcher to always return failure with the given reason.</summary>
    public MockTemporalWorkflowDispatcher ReturnsFailure(string reason = "Validation failed")
    {
        _handler = (input, _) => new IntegrationPipelineResult(input.MessageId, false, reason);
        return this;
    }

    /// <inheritdoc />
    public Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);

        _dispatches.Enqueue(new DispatchRecord(input, workflowId, DateTimeOffset.UtcNow));

        var result = _handler is not null
            ? _handler(input, workflowId)
            : new IntegrationPipelineResult(input.MessageId, true);

        return Task.FromResult(result);
    }

    /// <summary>Gets the most recent dispatch input, or null if none.</summary>
    public IntegrationPipelineInput? LastInput =>
        _dispatches.LastOrDefault()?.Input;

    /// <summary>Gets the most recent dispatch workflow ID, or null if none.</summary>
    public string? LastWorkflowId =>
        _dispatches.LastOrDefault()?.WorkflowId;

    /// <summary>Gets the dispatch input at the specified index.</summary>
    public IntegrationPipelineInput GetInput(int index = 0) =>
        _dispatches.ElementAt(index).Input;

    /// <summary>Gets the workflow ID at the specified index.</summary>
    public string GetWorkflowId(int index = 0) =>
        _dispatches.ElementAt(index).WorkflowId;

    /// <summary>Asserts the total number of dispatches.</summary>
    public void AssertDispatchCount(int expected) =>
        NUnit.Framework.Assert.That(_dispatches.Count, NUnit.Framework.Is.EqualTo(expected),
            $"Expected {expected} dispatch(es), but received {_dispatches.Count}");

    /// <summary>Resets all recorded dispatches.</summary>
    public void Reset()
    {
        while (_dispatches.TryDequeue(out _)) { }
    }

    public sealed record DispatchRecord(
        IntegrationPipelineInput Input,
        string WorkflowId,
        DateTimeOffset DispatchedAt);
}
