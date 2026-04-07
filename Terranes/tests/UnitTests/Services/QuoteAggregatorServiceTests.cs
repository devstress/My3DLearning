using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Abstractions;
using Terranes.Journey;
using Terranes.Quoting;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class QuoteAggregatorServiceTests
{
    private QuoteAggregatorService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var quotingService = new QuotingService(NullLogger<QuotingService>.Instance);
        _sut = new QuoteAggregatorService(quotingService, NullLogger<QuoteAggregatorService>.Instance);
    }

    // ── 1. Aggregation ──

    [Test]
    public async Task AggregateAsync_ValidJourney_ReturnsQuoteWithAllCategories()
    {
        var journeyId = Guid.NewGuid();
        var quote = await _sut.AggregateAsync(journeyId);

        Assert.That(quote.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(quote.JourneyId, Is.EqualTo(journeyId));
        Assert.That(quote.BuilderEstimateAud, Is.GreaterThan(0));
        Assert.That(quote.LandscapingEstimateAud, Is.GreaterThan(0));
        Assert.That(quote.FurnitureEstimateAud, Is.GreaterThan(0));
        Assert.That(quote.SmartHomeEstimateAud, Is.GreaterThan(0));
        Assert.That(quote.SolicitorEstimateAud, Is.GreaterThan(0));
    }

    [Test]
    public async Task AggregateAsync_TotalEqualsComponentSum()
    {
        var quote = await _sut.AggregateAsync(Guid.NewGuid());

        var expectedTotal = quote.BuilderEstimateAud + quote.LandscapingEstimateAud
            + quote.FurnitureEstimateAud + quote.SmartHomeEstimateAud + quote.SolicitorEstimateAud;

        Assert.That(quote.TotalEstimateAud, Is.EqualTo(expectedTotal));
    }

    [Test]
    public void AggregateAsync_EmptyJourneyId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AggregateAsync(Guid.Empty));
    }

    // ── 2. Retrieval ──

    [Test]
    public async Task GetAggregatedQuoteAsync_ExistingQuote_ReturnsIt()
    {
        var quote = await _sut.AggregateAsync(Guid.NewGuid());
        var retrieved = await _sut.GetAggregatedQuoteAsync(quote.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(quote.Id));
    }

    [Test]
    public async Task GetAggregatedQuoteAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetAggregatedQuoteAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 3. Journey Queries ──

    [Test]
    public async Task GetQuotesForJourneyAsync_MultipleQuotes_ReturnsAll()
    {
        var journeyId = Guid.NewGuid();
        await _sut.AggregateAsync(journeyId);
        await _sut.AggregateAsync(journeyId);

        var quotes = await _sut.GetQuotesForJourneyAsync(journeyId);
        Assert.That(quotes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetQuotesForJourneyAsync_NoQuotes_ReturnsEmpty()
    {
        var quotes = await _sut.GetQuotesForJourneyAsync(Guid.NewGuid());
        Assert.That(quotes, Is.Empty);
    }

    [Test]
    public async Task AggregateAsync_BuilderEstimate_IsInReasonableRange()
    {
        var quote = await _sut.AggregateAsync(Guid.NewGuid());

        Assert.That(quote.BuilderEstimateAud, Is.GreaterThanOrEqualTo(280_000));
        Assert.That(quote.BuilderEstimateAud, Is.LessThanOrEqualTo(400_001));
    }

    [Test]
    public async Task AggregateAsync_SolicitorEstimate_IsInReasonableRange()
    {
        var quote = await _sut.AggregateAsync(Guid.NewGuid());

        Assert.That(quote.SolicitorEstimateAud, Is.GreaterThanOrEqualTo(2_500));
        Assert.That(quote.SolicitorEstimateAud, Is.LessThanOrEqualTo(5_001));
    }
}
