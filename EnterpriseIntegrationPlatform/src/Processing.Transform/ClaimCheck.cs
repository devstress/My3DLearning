using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Claim Check — stores a large payload externally and replaces it with
/// a claim token (reference). The receiver retrieves the payload using
/// the token. Equivalent to BizTalk large-message handling via
/// streaming or external storage.
/// </summary>
public interface IClaimCheckStore
{
    /// <summary>Stores the payload and returns a claim token.</summary>
    Task<string> CheckInAsync(byte[] payload, CancellationToken ct = default);

    /// <summary>Retrieves the payload for the given claim token.</summary>
    Task<byte[]> CheckOutAsync(string claimToken, CancellationToken ct = default);
}

/// <summary>
/// In-memory claim check store for development and testing.
/// Production implementation would use blob storage or a database.
/// </summary>
public sealed class InMemoryClaimCheckStore : IClaimCheckStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    /// <inheritdoc />
    public Task<string> CheckInAsync(byte[] payload, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        _store[token] = payload;
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<byte[]> CheckOutAsync(string claimToken, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(claimToken, out var payload))
            throw new KeyNotFoundException($"No payload found for claim token '{claimToken}'.");

        return Task.FromResult(payload);
    }
}
