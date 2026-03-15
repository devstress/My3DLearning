using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// A single step in a processing pipeline.
/// Equivalent to a BizTalk pipeline component in Receive/Send pipelines.
/// </summary>
public interface IPipelineStep<T>
{
    /// <summary>Processes the envelope and returns the (possibly modified) result.</summary>
    Task<IntegrationEnvelope<T>> ProcessAsync(
        IntegrationEnvelope<T> envelope, CancellationToken ct = default);
}

/// <summary>
/// Pipes and Filters — chains multiple processing steps into a sequential pipeline.
/// Each step receives the output of the previous step.
/// Equivalent to BizTalk Receive Pipeline (Decode → Disassemble → Validate → ResolveParty)
/// and Send Pipeline (Pre-Assemble → Assemble → Encode).
/// </summary>
public sealed class Pipeline<T>
{
    private readonly List<IPipelineStep<T>> _steps = new();

    /// <summary>Appends a processing step to the pipeline.</summary>
    public Pipeline<T> AddStep(IPipelineStep<T> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>Appends a delegate-based processing step.</summary>
    public Pipeline<T> AddStep(
        Func<IntegrationEnvelope<T>, CancellationToken, Task<IntegrationEnvelope<T>>> process)
    {
        _steps.Add(new DelegatePipelineStep<T>(process));
        return this;
    }

    /// <summary>Executes all steps in sequence, passing the result of each to the next.</summary>
    public async Task<IntegrationEnvelope<T>> ExecuteAsync(
        IntegrationEnvelope<T> input, CancellationToken ct = default)
    {
        var current = input;
        foreach (var step in _steps)
        {
            current = await step.ProcessAsync(current, ct);
        }
        return current;
    }
}

internal sealed class DelegatePipelineStep<T> : IPipelineStep<T>
{
    private readonly Func<IntegrationEnvelope<T>, CancellationToken, Task<IntegrationEnvelope<T>>> _process;

    public DelegatePipelineStep(
        Func<IntegrationEnvelope<T>, CancellationToken, Task<IntegrationEnvelope<T>>> process) =>
        _process = process;

    public Task<IntegrationEnvelope<T>> ProcessAsync(
        IntegrationEnvelope<T> envelope, CancellationToken ct = default) =>
        _process(envelope, ct);
}
