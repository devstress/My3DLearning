namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Maps Ack/Nack notification payloads to the format required by the downstream subscriber.
/// <para>
/// In the Enterprise Integration Patterns book, the <b>Channel Adapter</b> bridges the
/// application to the messaging system. When a Channel Adapter delivers a message
/// successfully, the platform publishes an Ack; when delivery fails (timeout, error),
/// it publishes a Nack. This mapper controls the <em>format</em> of that notification —
/// for example, XML (<c>&lt;Ack&gt;ok&lt;/Ack&gt;</c>) or JSON.
/// </para>
/// <para>
/// Different integrations may require different notification formats depending on what
/// the originating sender expects. Register a custom implementation to change the format
/// per integration or per tenant.
/// </para>
/// </summary>
public interface INotificationMapper
{
    /// <summary>
    /// Maps a successful Channel Adapter delivery to an Ack notification payload string.
    /// </summary>
    /// <param name="messageId">The delivered message identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <returns>The formatted Ack payload (e.g. <c>&lt;Ack&gt;ok&lt;/Ack&gt;</c>).</returns>
    string MapAck(Guid messageId, Guid correlationId);

    /// <summary>
    /// Maps a Channel Adapter delivery failure to a Nack notification payload string.
    /// </summary>
    /// <param name="messageId">The faulted message identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="errorMessage">The error message describing the failure (e.g. timeout, connection refused).</param>
    /// <returns>The formatted Nack payload (e.g. <c>&lt;Nack&gt;not ok because of timeout&lt;/Nack&gt;</c>).</returns>
    string MapNack(Guid messageId, Guid correlationId, string errorMessage);
}
