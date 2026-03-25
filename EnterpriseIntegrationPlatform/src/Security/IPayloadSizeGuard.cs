namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Enforces a maximum byte size on serialized message payloads.
/// </summary>
public interface IPayloadSizeGuard
{
    /// <summary>
    /// Throws <see cref="PayloadTooLargeException"/> when the UTF-8 encoded size of
    /// <paramref name="payload"/> exceeds <see cref="PayloadSizeOptions.MaxPayloadBytes"/>.
    /// </summary>
    /// <param name="payload">The string payload to evaluate.</param>
    /// <exception cref="PayloadTooLargeException">
    /// Thrown when the payload exceeds the configured maximum.
    /// </exception>
    void Enforce(string payload);

    /// <summary>
    /// Throws <see cref="PayloadTooLargeException"/> when the byte array
    /// exceeds <see cref="PayloadSizeOptions.MaxPayloadBytes"/>.
    /// </summary>
    /// <param name="payloadBytes">The raw bytes to evaluate.</param>
    /// <exception cref="PayloadTooLargeException">
    /// Thrown when the payload exceeds the configured maximum.
    /// </exception>
    void Enforce(byte[] payloadBytes);
}
