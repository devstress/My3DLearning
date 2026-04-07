using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Immersive3D;

/// <summary>
/// In-memory implementation of <see cref="IDesignEditorService"/>.
/// Supports real-time 3D edits with undo and edit history.
/// </summary>
public sealed class DesignEditorService : IDesignEditorService
{
    private readonly ConcurrentDictionary<Guid, DesignEdit> _edits = new();
    private readonly ILogger<DesignEditorService> _logger;

    public DesignEditorService(ILogger<DesignEditorService> logger) => _logger = logger;

    public Task<DesignEdit> ApplyEditAsync(DesignEdit edit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);

        if (edit.SitePlacementId == Guid.Empty)
            throw new ArgumentException("Site placement ID is required.", nameof(edit));

        if (string.IsNullOrWhiteSpace(edit.PropertyName))
            throw new ArgumentException("Property name is required.", nameof(edit));

        if (string.IsNullOrWhiteSpace(edit.NewValue))
            throw new ArgumentException("New value is required.", nameof(edit));

        var persisted = edit with { Id = edit.Id == Guid.Empty ? Guid.NewGuid() : edit.Id, EditedAtUtc = DateTimeOffset.UtcNow };

        if (!_edits.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Edit {persisted.Id} already exists.");

        _logger.LogInformation("Applied {Operation} edit to placement {PlacementId}", persisted.Operation, persisted.SitePlacementId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<DesignEdit>> GetEditHistoryAsync(Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DesignEdit> result = _edits.Values
            .Where(e => e.SitePlacementId == sitePlacementId)
            .OrderBy(e => e.EditedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<DesignEdit?> UndoLastEditAsync(Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        var lastEdit = _edits.Values
            .Where(e => e.SitePlacementId == sitePlacementId)
            .OrderByDescending(e => e.EditedAtUtc)
            .FirstOrDefault();

        if (lastEdit is null)
            return Task.FromResult<DesignEdit?>(null);

        _edits.TryRemove(lastEdit.Id, out _);

        _logger.LogInformation("Undid {Operation} edit {EditId} for placement {PlacementId}", lastEdit.Operation, lastEdit.Id, sitePlacementId);
        return Task.FromResult<DesignEdit?>(lastEdit);
    }

    public Task<IReadOnlyList<DesignEdit>> GetEditsByTypeAsync(Guid sitePlacementId, EditOperationType operation, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DesignEdit> result = _edits.Values
            .Where(e => e.SitePlacementId == sitePlacementId && e.Operation == operation)
            .OrderBy(e => e.EditedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> ResetEditsAsync(Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        var toRemove = _edits.Values.Where(e => e.SitePlacementId == sitePlacementId).ToList();
        foreach (var edit in toRemove)
            _edits.TryRemove(edit.Id, out _);

        _logger.LogInformation("Reset {Count} edits for placement {PlacementId}", toRemove.Count, sitePlacementId);
        return Task.FromResult(toRemove.Count);
    }
}
