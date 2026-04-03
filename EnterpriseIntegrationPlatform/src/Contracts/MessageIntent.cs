namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Indicates the intent of a message, distinguishing between the three fundamental
/// message types defined by the Enterprise Integration Patterns:
/// Command Message, Document Message, and Event Message.
/// </summary>
public enum MessageIntent
{
    /// <summary>
    /// A command message instructs the receiver to perform a specific action.
    /// The sender expects the receiver to carry out the requested operation.
    /// </summary>
    Command = 0,

    /// <summary>
    /// A document message carries data for the receiver to process.
    /// The sender does not dictate what the receiver should do with the data.
    /// </summary>
    Document = 1,

    /// <summary>
    /// An event message notifies the receiver that something has occurred.
    /// The sender publishes the fact; it does not know or care who consumes it.
    /// </summary>
    Event = 2,
}
