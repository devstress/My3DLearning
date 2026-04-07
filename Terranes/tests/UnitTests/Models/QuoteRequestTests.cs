using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.UnitTests.Models;

[TestFixture]
public sealed class QuoteRequestTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesRecord()
    {
        var id = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.UtcNow;

        var request = new QuoteRequest(
            Id: id,
            SitePlacementId: placementId,
            RequestedByUserId: userId,
            Status: QuoteStatus.Pending,
            RequestedAtUtc: requestedAt);

        Assert.That(request.Id, Is.EqualTo(id));
        Assert.That(request.SitePlacementId, Is.EqualTo(placementId));
        Assert.That(request.Status, Is.EqualTo(QuoteStatus.Pending));
    }

    [Test]
    public void With_StatusChange_CreatesNewRecordWithUpdatedStatus()
    {
        var request = new QuoteRequest(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            QuoteStatus.Pending, DateTimeOffset.UtcNow);

        var updated = request with { Status = QuoteStatus.Completed };

        Assert.That(updated.Status, Is.EqualTo(QuoteStatus.Completed));
        Assert.That(updated.Id, Is.EqualTo(request.Id));
    }
}
