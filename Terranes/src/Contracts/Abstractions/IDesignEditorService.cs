using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for real-time 3D editing — modify home design on-block with undo support.
/// </summary>
public interface IDesignEditorService
{
    /// <summary>Applies a design edit to a site placement.</summary>
    Task<DesignEdit> ApplyEditAsync(DesignEdit edit, CancellationToken cancellationToken = default);

    /// <summary>Gets the full edit history for a site placement.</summary>
    Task<IReadOnlyList<DesignEdit>> GetEditHistoryAsync(Guid sitePlacementId, CancellationToken cancellationToken = default);

    /// <summary>Undoes the last edit for a site placement.</summary>
    Task<DesignEdit?> UndoLastEditAsync(Guid sitePlacementId, CancellationToken cancellationToken = default);

    /// <summary>Gets edits filtered by operation type.</summary>
    Task<IReadOnlyList<DesignEdit>> GetEditsByTypeAsync(Guid sitePlacementId, EditOperationType operation, CancellationToken cancellationToken = default);

    /// <summary>Clears all edits for a site placement (reset to original).</summary>
    Task<int> ResetEditsAsync(Guid sitePlacementId, CancellationToken cancellationToken = default);
}
