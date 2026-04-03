using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporalio.Client;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Connects to Temporal and starts the <c>IntegrationPipelineWorkflow</c>
/// using the Temporal .NET client. The entire pipeline (persist → validate →
/// ack/nack) runs atomically inside Temporal — this dispatcher is a thin
/// pass-through that starts the workflow and awaits its result.
/// </summary>
public sealed class TemporalWorkflowDispatcher : ITemporalWorkflowDispatcher, IAsyncDisposable
{
    private readonly PipelineOptions _options;
    private readonly ILogger<TemporalWorkflowDispatcher> _logger;
    private ITemporalClient? _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>Initialises a new instance of <see cref="TemporalWorkflowDispatcher"/>.</summary>
    public TemporalWorkflowDispatcher(
        IOptions<PipelineOptions> options,
        ILogger<TemporalWorkflowDispatcher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IntegrationPipelineResult> DispatchAsync(
        IntegrationPipelineInput input,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);

        var client = await EnsureClientAsync(cancellationToken);

        _logger.LogDebug(
            "Starting IntegrationPipelineWorkflow {WorkflowId} for message {MessageId}",
            workflowId, input.MessageId);

        // Use string-based API so this project does not need a project reference
        // to Workflow.Temporal. The workflow type name matches the [Workflow] class name.
        var handle = await client.StartWorkflowAsync(
            "IntegrationPipelineWorkflow",
            (IReadOnlyCollection<object>)[input],
            new WorkflowOptions(id: workflowId, taskQueue: _options.TemporalTaskQueue)
            {
                ExecutionTimeout = _options.WorkflowTimeout,
            });

        var result = await handle.GetResultAsync<IntegrationPipelineResult>(
            rpcOptions: new RpcOptions { CancellationToken = cancellationToken });

        _logger.LogDebug(
            "Workflow {WorkflowId} completed: IsSuccess={IsSuccess}",
            workflowId, result.IsSuccess);

        return result;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        if (_client is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    private async Task<ITemporalClient> EnsureClientAsync(CancellationToken ct)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_client is null)
            {
                _logger.LogInformation(
                    "Connecting to Temporal at {Address} (namespace={Namespace})",
                    _options.TemporalServerAddress, _options.TemporalNamespace);

                _client = await TemporalClient.ConnectAsync(
                    new TemporalClientConnectOptions(_options.TemporalServerAddress)
                    {
                        Namespace = _options.TemporalNamespace,
                    });

                _logger.LogInformation("Temporal client connected");
            }
        }
        finally
        {
            _initLock.Release();
        }

        return _client;
    }
}
