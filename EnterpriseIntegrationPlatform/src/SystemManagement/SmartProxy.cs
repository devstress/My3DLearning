using System.Collections.Concurrent;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Production implementation of the Smart Proxy Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Tracks outstanding request-reply correlations in a thread-safe
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="IntegrationEnvelope{T}.CorrelationId"/>. When a reply arrives,
/// the proxy looks up the correlation identifier, retrieves the original
/// requester's return address, and removes the tracking entry.
/// </para>
/// </remarks>
public sealed class SmartProxy : ISmartProxy
{
    private readonly ConcurrentDictionary<Guid, SmartProxyTrackingEntry> _outstanding = new();
    private readonly ILogger<SmartProxy> _logger;

    /// <summary>Initialises a new instance of <see cref="SmartProxy"/>.</summary>
    public SmartProxy(ILogger<SmartProxy> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public int OutstandingCount => _outstanding.Count;

    /// <inheritdoc />
    public bool TrackRequest<T>(IntegrationEnvelope<T> requestEnvelope)
    {
        ArgumentNullException.ThrowIfNull(requestEnvelope);

        if (string.IsNullOrWhiteSpace(requestEnvelope.ReplyTo))
        {
            _logger.LogWarning(
                "Cannot track request {MessageId} — no ReplyTo address set",
                requestEnvelope.MessageId);
            return false;
        }

        var entry = new SmartProxyTrackingEntry(
            requestEnvelope.MessageId,
            requestEnvelope.ReplyTo,
            DateTimeOffset.UtcNow);

        var added = _outstanding.TryAdd(requestEnvelope.CorrelationId, entry);

        if (added)
        {
            _logger.LogDebug(
                "Smart Proxy tracking request {MessageId} (CorrelationId={CorrelationId}, ReplyTo={ReplyTo})",
                requestEnvelope.MessageId, requestEnvelope.CorrelationId, requestEnvelope.ReplyTo);
        }
        else
        {
            _logger.LogWarning(
                "Smart Proxy: CorrelationId {CorrelationId} already tracked — duplicate request {MessageId}",
                requestEnvelope.CorrelationId, requestEnvelope.MessageId);
        }

        return added;
    }

    /// <inheritdoc />
    public SmartProxyCorrelation? CorrelateReply<T>(IntegrationEnvelope<T> replyEnvelope)
    {
        ArgumentNullException.ThrowIfNull(replyEnvelope);

        if (_outstanding.TryRemove(replyEnvelope.CorrelationId, out var entry))
        {
            _logger.LogDebug(
                "Smart Proxy correlated reply {MessageId} to request {RequestMessageId} (CorrelationId={CorrelationId})",
                replyEnvelope.MessageId, entry.RequestMessageId, replyEnvelope.CorrelationId);

            return new SmartProxyCorrelation(
                replyEnvelope.CorrelationId,
                entry.OriginalReplyTo,
                entry.RequestMessageId);
        }

        _logger.LogWarning(
            "Smart Proxy: no outstanding request for reply {MessageId} (CorrelationId={CorrelationId})",
            replyEnvelope.MessageId, replyEnvelope.CorrelationId);

        return null;
    }

    /// <summary>
    /// Internal tracking entry for an outstanding request.
    /// </summary>
    internal sealed record SmartProxyTrackingEntry(
        Guid RequestMessageId,
        string OriginalReplyTo,
        DateTimeOffset TrackedAt);
}
