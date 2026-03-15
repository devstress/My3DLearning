using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Interface for content-based message routing.
/// Routes messages to different destinations based on message content,
/// type, headers, or metadata — the core BizTalk routing pattern.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Determines the destination(s) for a message based on its content.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message to route.</param>
    /// <returns>Destination identifiers for the message.</returns>
    IReadOnlyList<string> Route<T>(IntegrationEnvelope<T> envelope);
}
