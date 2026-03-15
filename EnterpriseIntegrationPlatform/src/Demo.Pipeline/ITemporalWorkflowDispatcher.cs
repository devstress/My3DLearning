using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Sends an integration message to the Temporal workflow cluster and returns
/// the workflow result. The Temporal connection is established lazily and cached
/// for the lifetime of the dispatcher.
/// </summary>
public interface ITemporalWorkflowDispatcher
{
    /// <summary>
    /// Starts the <c>ProcessIntegrationMessageWorkflow</c> and awaits its completion.
    /// </summary>
    /// <param name="input">Workflow input produced from the inbound message envelope.</param>
    /// <param name="workflowId">
    /// Unique workflow identifier — typically derived from the message ID so that
    /// resubmitting the same message produces an idempotent outcome.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    Task<ProcessIntegrationMessageResult> DispatchAsync(
        ProcessIntegrationMessageInput input,
        string workflowId,
        CancellationToken cancellationToken = default);
}
