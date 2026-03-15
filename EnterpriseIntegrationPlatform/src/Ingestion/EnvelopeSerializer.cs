using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Serialises and deserialises <see cref="IntegrationEnvelope{T}"/> instances
/// for transport over message brokers.
/// </summary>
public static class EnvelopeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Serialises an envelope to a UTF-8 byte array.</summary>
    public static byte[] Serialize<T>(IntegrationEnvelope<T> envelope) =>
        JsonSerializer.SerializeToUtf8Bytes(envelope, Options);

    /// <summary>Deserialises an envelope from a UTF-8 byte array.</summary>
    public static IntegrationEnvelope<T>? Deserialize<T>(ReadOnlySpan<byte> data) =>
        JsonSerializer.Deserialize<IntegrationEnvelope<T>>(data, Options);
}
