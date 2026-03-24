namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Exception thrown when an inbound message payload exceeds the configured size limit.
/// </summary>
public sealed class PayloadTooLargeException : Exception
{
    /// <summary>The actual size of the payload in bytes.</summary>
    public int ActualBytes { get; }

    /// <summary>The maximum allowed payload size in bytes.</summary>
    public int MaxBytes { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="PayloadTooLargeException"/>.
    /// </summary>
    /// <param name="actualBytes">Actual payload size in bytes.</param>
    /// <param name="maxBytes">Configured maximum payload size in bytes.</param>
    public PayloadTooLargeException(int actualBytes, int maxBytes)
        : base($"Payload size {actualBytes} bytes exceeds the maximum allowed {maxBytes} bytes.")
    {
        ActualBytes = actualBytes;
        MaxBytes = maxBytes;
    }
}
