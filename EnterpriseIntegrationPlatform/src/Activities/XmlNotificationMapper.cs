using System.Security;

namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Maps Ack/Nack notification payloads to XML format as expected by
/// downstream subscribers that consume Channel Adapter delivery outcomes.
/// <para>
/// Ack format:  <c>&lt;Ack&gt;ok&lt;/Ack&gt;</c><br/>
/// Nack format: <c>&lt;Nack&gt;not ok because of {ErrorMessage}&lt;/Nack&gt;</c>
/// </para>
/// </summary>
public sealed class XmlNotificationMapper : INotificationMapper
{
    /// <inheritdoc />
    public string MapAck(Guid messageId, Guid correlationId)
    {
        return "<Ack>ok</Ack>";
    }

    /// <inheritdoc />
    public string MapNack(Guid messageId, Guid correlationId, string errorMessage)
    {
        var escaped = SecurityElement.Escape(errorMessage);
        return $"<Nack>not ok because of {escaped}</Nack>";
    }
}
