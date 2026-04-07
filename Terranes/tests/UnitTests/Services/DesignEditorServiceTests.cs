using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Immersive3D;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class DesignEditorServiceTests
{
    private DesignEditorService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new DesignEditorService(NullLogger<DesignEditorService>.Instance);

    // ── 1. Apply Edits ──

    [Test]
    public async Task ApplyEditAsync_ValidEdit_ReturnsWithId()
    {
        var edit = MakeEdit();
        var created = await _sut.ApplyEditAsync(edit);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Operation, Is.EqualTo(EditOperationType.MaterialChange));
        Assert.That(created.NewValue, Is.EqualTo("Oak Timber"));
    }

    [Test]
    public void ApplyEditAsync_EmptyPlacementId_ThrowsArgumentException()
    {
        var edit = MakeEdit() with { SitePlacementId = Guid.Empty };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ApplyEditAsync(edit));
    }

    [Test]
    public void ApplyEditAsync_EmptyPropertyName_ThrowsArgumentException()
    {
        var edit = MakeEdit() with { PropertyName = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ApplyEditAsync(edit));
    }

    [Test]
    public void ApplyEditAsync_EmptyNewValue_ThrowsArgumentException()
    {
        var edit = MakeEdit() with { NewValue = "" };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ApplyEditAsync(edit));
    }

    // ── 2. Edit History ──

    [Test]
    public async Task GetEditHistoryAsync_ReturnsChronologicalOrder()
    {
        var placementId = Guid.NewGuid();
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, NewValue = "First" });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, NewValue = "Second" });

        var history = await _sut.GetEditHistoryAsync(placementId);

        Assert.That(history, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetEditsByTypeAsync_FiltersCorrectly()
    {
        var placementId = Guid.NewGuid();
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, Operation = EditOperationType.MaterialChange });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, Operation = EditOperationType.Move, PropertyName = "position" });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, Operation = EditOperationType.MaterialChange, PropertyName = "wall" });

        var materialEdits = await _sut.GetEditsByTypeAsync(placementId, EditOperationType.MaterialChange);
        Assert.That(materialEdits, Has.Count.EqualTo(2));
    }

    // ── 3. Undo & Reset ──

    [Test]
    public async Task UndoLastEditAsync_RemovesLastEdit()
    {
        var placementId = Guid.NewGuid();
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, NewValue = "First" });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId, NewValue = "Second" });

        var undone = await _sut.UndoLastEditAsync(placementId);
        Assert.That(undone, Is.Not.Null);
        Assert.That(undone!.NewValue, Is.EqualTo("Second"));

        var remaining = await _sut.GetEditHistoryAsync(placementId);
        Assert.That(remaining, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UndoLastEditAsync_NoEdits_ReturnsNull()
    {
        var result = await _sut.UndoLastEditAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ResetEditsAsync_ClearsAllEdits()
    {
        var placementId = Guid.NewGuid();
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId });
        await _sut.ApplyEditAsync(MakeEdit() with { SitePlacementId = placementId });

        var removed = await _sut.ResetEditsAsync(placementId);
        Assert.That(removed, Is.EqualTo(3));

        var remaining = await _sut.GetEditHistoryAsync(placementId);
        Assert.That(remaining, Is.Empty);
    }

    [Test]
    public async Task ResetEditsAsync_NoEdits_ReturnsZero()
    {
        var removed = await _sut.ResetEditsAsync(Guid.NewGuid());
        Assert.That(removed, Is.EqualTo(0));
    }

    private static DesignEdit MakeEdit() => new(
        Guid.Empty, Guid.NewGuid(), EditOperationType.MaterialChange,
        "FloorMaterial", "Carpet", "Oak Timber",
        Guid.NewGuid(), default);
}
