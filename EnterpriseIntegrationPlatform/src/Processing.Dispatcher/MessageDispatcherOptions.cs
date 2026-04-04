namespace EnterpriseIntegrationPlatform.Processing.Dispatcher;

/// <summary>
/// Configuration options for the <see cref="MessageDispatcher"/>.
/// </summary>
public sealed class MessageDispatcherOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "MessageDispatcher";

    /// <summary>
    /// When <c>true</c>, dispatching a message whose type has no registered handler
    /// throws <see cref="InvalidOperationException"/> instead of returning a
    /// <see cref="DispatchResult"/> with <c>HandlerFound = false</c>.
    /// Default is <c>false</c>.
    /// </summary>
    public bool ThrowOnUnknownType { get; set; }
}
