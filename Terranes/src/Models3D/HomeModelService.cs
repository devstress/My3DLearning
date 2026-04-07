using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Models3D;

/// <summary>
/// In-memory implementation of <see cref="IHomeModelService"/>.
/// Manages 3D home model upload, retrieval, validation, and search.
/// </summary>
public sealed class HomeModelService : IHomeModelService
{
    private static readonly HashSet<ModelFormat> SupportedFormats =
    [
        ModelFormat.Gltf, ModelFormat.Glb, ModelFormat.Obj, ModelFormat.Fbx, ModelFormat.Usd
    ];

    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB

    private readonly ConcurrentDictionary<Guid, HomeModel> _store = new();
    private readonly ILogger<HomeModelService> _logger;

    public HomeModelService(ILogger<HomeModelService> logger) => _logger = logger;

    public Task<HomeModel> CreateAsync(HomeModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Home model name is required.", nameof(model));

        if (!SupportedFormats.Contains(model.Format))
            throw new ArgumentException($"Unsupported model format: {model.Format}.", nameof(model));

        if (model.FileSizeBytes <= 0 || model.FileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"File size must be between 1 byte and {MaxFileSizeBytes} bytes.", nameof(model));

        if (model.Bedrooms < 0)
            throw new ArgumentException("Bedrooms cannot be negative.", nameof(model));

        if (model.FloorAreaSquareMetres <= 0)
            throw new ArgumentException("Floor area must be positive.", nameof(model));

        var persisted = model with { Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, CreatedAtUtc = DateTimeOffset.UtcNow };

        if (!_store.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Home model with ID {persisted.Id} already exists.");

        _logger.LogInformation("Created home model {ModelId} ({Format})", persisted.Id, persisted.Format);
        return Task.FromResult(persisted);
    }

    public Task<HomeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task<IReadOnlyList<HomeModel>> SearchAsync(int? minBedrooms = null, ModelFormat? format = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<HomeModel> query = _store.Values;

        if (minBedrooms.HasValue)
            query = query.Where(m => m.Bedrooms >= minBedrooms.Value);

        if (format.HasValue)
            query = query.Where(m => m.Format == format.Value);

        IReadOnlyList<HomeModel> result = query.OrderByDescending(m => m.CreatedAtUtc).ToList();
        return Task.FromResult(result);
    }
}
