using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Tracks outstanding request-reply interactions and correlates Return Address
/// responses. Implements the Smart Proxy Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The Smart Proxy sits between the requester and the replier, intercepting
/// outbound requests and tracking their Return Address correlation identifiers.
/// When replies arrive, the proxy correlates them to the original request and
/// forwards the reply to the requester.
/// </para>
/// </remarks>
public interface ISmartProxy
{
    /// <summary>
    /// Tracks an outbound request, recording its correlation identifier and
    /// reply-to address for later correlation.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="requestEnvelope">The outbound request envelope.</param>
    /// <returns><c>true</c> if the request was tracked; <c>false</c> if already tracked.</returns>
    bool TrackRequest<T>(IntegrationEnvelope<T> requestEnvelope);

    /// <summary>
    /// Attempts to correlate an inbound reply to a previously tracked request.
    /// On match, returns the original requester's reply-to address and removes
    /// the tracking entry.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="replyEnvelope">The inbound reply envelope.</param>
    /// <returns>
    /// A <see cref="SmartProxyCorrelation"/> if a match was found; otherwise <c>null</c>.
    /// </returns>
    SmartProxyCorrelation? CorrelateReply<T>(IntegrationEnvelope<T> replyEnvelope);

    /// <summary>
    /// Returns the number of outstanding (unmatched) requests.
    /// </summary>
    int OutstandingCount { get; }
}
