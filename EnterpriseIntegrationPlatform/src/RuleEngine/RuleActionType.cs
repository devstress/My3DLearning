namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// The action to perform when a <see cref="BusinessRule"/> matches a message.
/// </summary>
public enum RuleActionType
{
    /// <summary>Route the message to a specified topic.</summary>
    Route,

    /// <summary>Apply a named transform to the message payload.</summary>
    Transform,

    /// <summary>Reject the message, preventing further processing.</summary>
    Reject,

    /// <summary>Send the message to the dead letter queue.</summary>
    DeadLetter,
}
