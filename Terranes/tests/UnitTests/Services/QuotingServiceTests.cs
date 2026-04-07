using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Quoting;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class QuotingServiceTests
{
    private QuotingService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new QuotingService(NullLogger<QuotingService>.Instance);

    // ── 1. Quote Request ──

    [Test]
    public async Task RequestQuoteAsync_ValidRequest_ReturnsWithPendingStatus()
    {
        var request = MakeRequest();
        var created = await _sut.RequestQuoteAsync(request);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(QuoteStatus.Pending));
    }

    [Test]
    public void RequestQuoteAsync_EmptySitePlacementId_ThrowsArgumentException()
    {
        var request = MakeRequest() with { SitePlacementId = Guid.Empty };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RequestQuoteAsync(request));
    }

    [Test]
    public void RequestQuoteAsync_EmptyUserId_ThrowsArgumentException()
    {
        var request = MakeRequest() with { RequestedByUserId = Guid.Empty };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RequestQuoteAsync(request));
    }

    // ── 2. Line Items ──

    [Test]
    public async Task AddLineItemAsync_ValidItem_AddsToQuote()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        var item = MakeLineItem(quote.Id);

        var created = await _sut.AddLineItemAsync(item);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.QuoteRequestId, Is.EqualTo(quote.Id));
    }

    [Test]
    public async Task AddLineItemAsync_UpdatesStatusToInProgress()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id));

        var updated = await _sut.GetQuoteRequestAsync(quote.Id);
        Assert.That(updated!.Status, Is.EqualTo(QuoteStatus.InProgress));
    }

    [Test]
    public void AddLineItemAsync_NonExistentQuote_ThrowsInvalidOperationException()
    {
        var item = MakeLineItem(Guid.NewGuid());
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddLineItemAsync(item));
    }

    [Test]
    public async Task AddLineItemAsync_NegativeAmount_ThrowsArgumentException()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        var item = MakeLineItem(quote.Id) with { AmountAud = -100m };
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddLineItemAsync(item));
    }

    [Test]
    public async Task GetLineItemsAsync_MultipleItems_ReturnsAll()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.Builder, 350_000m));
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.Landscaper, 25_000m));
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.InteriorFurnishing, 40_000m));

        var items = await _sut.GetLineItemsAsync(quote.Id);
        Assert.That(items, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetLineItemsAsync_EmptyQuote_ReturnsEmptyList()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        var items = await _sut.GetLineItemsAsync(quote.Id);

        Assert.That(items, Is.Empty);
    }

    // ── 3. Completion ──

    [Test]
    public async Task CompleteQuoteAsync_SetsStatusToCompleted()
    {
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id));

        var completed = await _sut.CompleteQuoteAsync(quote.Id);
        Assert.That(completed.Status, Is.EqualTo(QuoteStatus.Completed));
    }

    [Test]
    public void CompleteQuoteAsync_NonExistentQuote_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CompleteQuoteAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetQuoteRequestAsync_ExistingQuote_ReturnsQuote()
    {
        var created = await _sut.RequestQuoteAsync(MakeRequest());
        var retrieved = await _sut.GetQuoteRequestAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.SitePlacementId, Is.EqualTo(created.SitePlacementId));
    }

    [Test]
    public async Task GetQuoteRequestAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetQuoteRequestAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task EndToEnd_FullQuoteLifecycle()
    {
        // Request → Add items → Complete
        var quote = await _sut.RequestQuoteAsync(MakeRequest());
        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Pending));

        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.Builder, 380_000m));
        var inProgress = await _sut.GetQuoteRequestAsync(quote.Id);
        Assert.That(inProgress!.Status, Is.EqualTo(QuoteStatus.InProgress));

        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.Landscaper, 30_000m));
        await _sut.AddLineItemAsync(MakeLineItem(quote.Id, PartnerCategory.SmartHome, 15_000m));

        var completed = await _sut.CompleteQuoteAsync(quote.Id);
        Assert.That(completed.Status, Is.EqualTo(QuoteStatus.Completed));

        var items = await _sut.GetLineItemsAsync(quote.Id);
        Assert.That(items, Has.Count.EqualTo(3));

        var total = items.Sum(i => i.AmountAud);
        Assert.That(total, Is.EqualTo(425_000m));
    }

    private static QuoteRequest MakeRequest() => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), QuoteStatus.Pending, default);

    private static QuoteLineItem MakeLineItem(Guid quoteId, PartnerCategory category = PartnerCategory.Builder, decimal amount = 350_000m) => new(
        Guid.Empty, quoteId, Guid.NewGuid(), category, amount,
        $"{category} quote", DateTimeOffset.UtcNow.AddDays(30), default);
}
