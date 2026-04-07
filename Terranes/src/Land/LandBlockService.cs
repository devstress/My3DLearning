using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Land;

/// <summary>
/// In-memory implementation of <see cref="ILandBlockService"/>.
/// Manages land block lookup, storage, and search by address or coordinates.
/// </summary>
public sealed class LandBlockService : ILandBlockService
{
    private readonly ConcurrentDictionary<Guid, LandBlock> _store = new();
    private readonly ILogger<LandBlockService> _logger;

    public LandBlockService(ILogger<LandBlockService> logger) => _logger = logger;

    /// <summary>
    /// Stores a land block and returns it with a generated ID if needed.
    /// </summary>
    public Task<LandBlock> CreateAsync(LandBlock block, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (string.IsNullOrWhiteSpace(block.Address))
            throw new ArgumentException("Address is required.", nameof(block));

        if (string.IsNullOrWhiteSpace(block.State))
            throw new ArgumentException("State is required.", nameof(block));

        if (block.AreaSquareMetres <= 0)
            throw new ArgumentException("Area must be positive.", nameof(block));

        if (block.FrontageMetres <= 0)
            throw new ArgumentException("Frontage must be positive.", nameof(block));

        if (block.DepthMetres <= 0)
            throw new ArgumentException("Depth must be positive.", nameof(block));

        var persisted = block with { Id = block.Id == Guid.Empty ? Guid.NewGuid() : block.Id };

        if (!_store.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Land block with ID {persisted.Id} already exists.");

        _logger.LogInformation("Created land block {BlockId} at {Address}, {State}", persisted.Id, persisted.Address, persisted.State);
        return Task.FromResult(persisted);
    }

    public Task<LandBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var block);
        return Task.FromResult(block);
    }

    public Task<LandBlock?> LookupByAddressAsync(string address, string state, CancellationToken cancellationToken = default)
    {
        var block = _store.Values.FirstOrDefault(b =>
            b.Address.Contains(address, StringComparison.OrdinalIgnoreCase) &&
            b.State.Equals(state, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(block);
    }

    /// <summary>
    /// Search for land blocks by suburb, state, or minimum area.
    /// </summary>
    public Task<IReadOnlyList<LandBlock>> SearchAsync(string? suburb = null, string? state = null, double? minAreaSqm = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<LandBlock> query = _store.Values;

        if (!string.IsNullOrWhiteSpace(suburb))
            query = query.Where(b => b.Suburb.Equals(suburb, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(b => b.State.Equals(state, StringComparison.OrdinalIgnoreCase));

        if (minAreaSqm.HasValue)
            query = query.Where(b => b.AreaSquareMetres >= minAreaSqm.Value);

        IReadOnlyList<LandBlock> result = query.OrderBy(b => b.Address).ToList();
        return Task.FromResult(result);
    }
}
