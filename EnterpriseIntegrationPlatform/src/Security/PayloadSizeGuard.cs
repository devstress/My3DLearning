using System.Text;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Production implementation of <see cref="IPayloadSizeGuard"/>.
/// </summary>
public sealed class PayloadSizeGuard : IPayloadSizeGuard
{
    private readonly PayloadSizeOptions _options;

    /// <summary>Initialises a new instance of <see cref="PayloadSizeGuard"/>.</summary>
    public PayloadSizeGuard(IOptions<PayloadSizeOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public void Enforce(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var byteCount = Encoding.UTF8.GetByteCount(payload);
        if (byteCount > _options.MaxPayloadBytes)
            throw new PayloadTooLargeException(byteCount, _options.MaxPayloadBytes);
    }

    /// <inheritdoc />
    public void Enforce(byte[] payloadBytes)
    {
        ArgumentNullException.ThrowIfNull(payloadBytes);
        if (payloadBytes.Length > _options.MaxPayloadBytes)
            throw new PayloadTooLargeException(payloadBytes.Length, _options.MaxPayloadBytes);
    }
}
