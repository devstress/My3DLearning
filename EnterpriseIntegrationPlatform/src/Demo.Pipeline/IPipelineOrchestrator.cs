using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Demo.Pipeline;

/// <summary>
/// Handles a single <see cref="IntegrationEnvelope{T}"/> through the full demo pipeline:
/// persist → dispatch to Temporal → publish Ack/Nack → update status.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>
    /// Processes one inbound message through the end-to-end demo pipeline.
    /// </summary>
    /// <param name="envelope">The inbound message envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessAsync(IntegrationEnvelope<JsonElement> envelope, CancellationToken cancellationToken = default);
}
