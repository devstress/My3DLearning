using System.Net;
using System.Net.Http.Json;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.IntegrationTests;

/// <summary>
/// Integration tests for core platform services exercised through the HTTP API.
/// Covers: HomeModel, LandBlock, SitePlacement, Quoting, Marketplace, Compliance endpoints.
/// </summary>
[TestFixture]
public sealed class CoreServiceIntegrationTests : IntegrationTestBase
{
    // ── 1. Home Models ──

    [Test]
    public async Task HomeModel_CreateAndRetrieve_RoundTrips()
    {
        var model = MakeHomeModel();
        var created = await PostAsync<HomeModel>("/api/home-models", model);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo(model.Name));

        var retrieved = await GetAsync<HomeModel>($"/api/home-models/{created.Id}");
        Assert.That(retrieved.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task HomeModel_GetNonExistent_Returns404()
    {
        var response = await Client.GetAsync($"/api/home-models/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task HomeModel_Search_ReturnsByBedrooms()
    {
        var m1 = MakeHomeModel() with { Bedrooms = 3 };
        var m2 = MakeHomeModel() with { Bedrooms = 5 };
        await PostAsync<HomeModel>("/api/home-models", m1);
        await PostAsync<HomeModel>("/api/home-models", m2);

        var results = await GetAsync<List<HomeModel>>("/api/home-models?minBedrooms=4");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(results.TrueForAll(r => r.Bedrooms >= 4));
    }

    // ── 2. Land Blocks ──

    [Test]
    public async Task LandBlock_CreateAndRetrieve_RoundTrips()
    {
        var block = MakeLandBlock();
        var created = await PostAsync<LandBlock>("/api/land-blocks", block);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var retrieved = await GetAsync<LandBlock>($"/api/land-blocks/{created.Id}");
        Assert.That(retrieved.Address, Is.EqualTo(block.Address));
    }

    [Test]
    public async Task LandBlock_Lookup_ByAddressAndState()
    {
        var block = MakeLandBlock();
        await PostAsync<LandBlock>("/api/land-blocks", block);

        var found = await GetAsync<LandBlock>($"/api/land-blocks/lookup?address={Uri.EscapeDataString(block.Address)}&state={block.State}");
        Assert.That(found.Address, Is.EqualTo(block.Address));
    }

    [Test]
    public async Task LandBlock_Search_BySuburb()
    {
        await PostAsync<LandBlock>("/api/land-blocks", MakeLandBlock());
        var results = await GetAsync<List<LandBlock>>("/api/land-blocks?suburb=Kellyville");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 3. Site Placements ──

    [Test]
    public async Task SitePlacement_CreateAndRetrieve_RoundTrips()
    {
        var placement = MakeSitePlacement();
        var created = await PostAsync<SitePlacement>("/api/site-placements", placement);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var retrieved = await GetAsync<SitePlacement>($"/api/site-placements/{created.Id}");
        Assert.That(retrieved.HomeModelId, Is.EqualTo(placement.HomeModelId));
    }

    [Test]
    public async Task SitePlacement_Validate_ReturnsResult()
    {
        var placement = MakeSitePlacement();
        var response = await Client.PostAsJsonAsync("/api/site-placements/validate", placement);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        Assert.That(body, Does.ContainKey("fits"));
    }

    // ── 4. Quoting ──

    [Test]
    public async Task Quoting_CreateAndComplete_Lifecycle()
    {
        var request = MakeQuoteRequest();
        var created = await PostAsync<QuoteRequest>("/api/quotes", request);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(QuoteStatus.Pending));

        // Add a line item
        var lineItem = MakeQuoteLineItem(created.Id);
        var createdItem = await PostAsync<QuoteLineItem>("/api/quotes/line-items", lineItem);
        Assert.That(createdItem.Id, Is.Not.EqualTo(Guid.Empty));

        // Get line items
        var items = await GetAsync<List<QuoteLineItem>>($"/api/quotes/{created.Id}/line-items");
        Assert.That(items, Has.Count.EqualTo(1));

        // Complete quote
        var response = await Client.PostAsync($"/api/quotes/{created.Id}/complete", null);
        response.EnsureSuccessStatusCode();
        var completed = await response.Content.ReadFromJsonAsync<QuoteRequest>();
        Assert.That(completed!.Status, Is.EqualTo(QuoteStatus.Completed));
    }

    // ── 5. Marketplace ──

    [Test]
    public async Task Marketplace_CreateAndSearchAndUpdateStatus()
    {
        var listing = MakeListing();
        var created = await PostAsync<PropertyListing>("/api/listings", listing);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(ListingStatus.Draft));

        // Search
        var results = await GetAsync<List<PropertyListing>>("/api/listings");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));

        // Update status
        var updated = await PutAsync<PropertyListing>($"/api/listings/{created.Id}/status?newStatus=Active");
        Assert.That(updated.Status, Is.EqualTo(ListingStatus.Active));
    }

    // ── 6. Compliance ──

    [Test]
    public async Task Compliance_CheckAndRetrieve()
    {
        // Create prerequisites
        var model = await PostAsync<HomeModel>("/api/home-models", MakeHomeModel());
        var block = await PostAsync<LandBlock>("/api/land-blocks", MakeLandBlock());
        var placement = await PostAsync<SitePlacement>("/api/site-placements",
            MakeSitePlacement() with { HomeModelId = model.Id, LandBlockId = block.Id });

        var checkRequest = new { SitePlacementId = placement.Id, Jurisdiction = "NSW" };
        var result = await PostAsync<ComplianceResult>("/api/compliance/check", checkRequest);

        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Jurisdiction, Is.EqualTo("NSW"));

        var retrieved = await GetAsync<ComplianceResult>($"/api/compliance/{result.Id}");
        Assert.That(retrieved.Id, Is.EqualTo(result.Id));

        var byPlacement = await GetAsync<List<ComplianceResult>>($"/api/compliance/placement/{placement.Id}");
        Assert.That(byPlacement, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 7. Health Check ──

    [Test]
    public async Task Health_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}. Body: {body}");

        var parsed = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.That(parsed, Does.ContainKey("status"));
    }

    // ── Factory Methods ──

    private static HomeModel MakeHomeModel() => new(
        Guid.Empty, "Modern Family Home", "Spacious 4-bed design",
        ModelFormat.Gltf, 5_000_000, 4, 2, 2, 220.0, Guid.NewGuid(), default);

    private static LandBlock MakeLandBlock() => new(
        Guid.Empty, "42 Sample Street", "Kellyville", "NSW", "2155",
        650.0, 20.0, 32.5, ZoningType.Residential, -33.7, 150.9);

    private static SitePlacement MakeSitePlacement() => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
        2.0, 3.0, 0.0, 1.0, Guid.NewGuid(), default);

    private static QuoteRequest MakeQuoteRequest() => new(
        Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), QuoteStatus.Pending, default);

    private static QuoteLineItem MakeQuoteLineItem(Guid quoteId) => new(
        Guid.Empty, quoteId, Guid.NewGuid(), PartnerCategory.Builder,
        350_000m, "Build to lock-up stage", DateTimeOffset.UtcNow.AddMonths(3), default);

    private static PropertyListing MakeListing() => new(
        Guid.Empty, Guid.NewGuid(), null, "Dream Home in Kellyville",
        "Beautiful 4-bed home with landscaping", 850_000m,
        ListingStatus.Draft, Guid.NewGuid(), default);
}
