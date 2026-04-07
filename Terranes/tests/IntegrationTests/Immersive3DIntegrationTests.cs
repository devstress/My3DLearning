using System.Net;
using System.Net.Http.Json;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.IntegrationTests;

/// <summary>
/// Integration tests for Immersive 3D + Infrastructure services through the HTTP API.
/// Covers: VirtualVillage, Walkthrough, DesignEditor, VideoToModel, Content, Auth, Observability, Tenant.
/// </summary>
[TestFixture]
public sealed class Immersive3DIntegrationTests : IntegrationTestBase
{
    // ── 1. Virtual Village ──

    [Test]
    public async Task Village_CreateAndRetrieve_RoundTrips()
    {
        var village = MakeVillage();
        var created = await PostAsync<VirtualVillage>("/api/villages", village);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo(village.Name));

        var retrieved = await GetAsync<VirtualVillage>($"/api/villages/{created.Id}");
        Assert.That(retrieved.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Village_AddLotAndGetStats()
    {
        var village = await PostAsync<VirtualVillage>("/api/villages", MakeVillage());
        var lot = MakeLot(village.Id);
        var createdLot = await PostAsync<VillageLot>($"/api/villages/{village.Id}/lots", lot);

        Assert.That(createdLot.Status, Is.EqualTo(VillageLotStatus.Vacant));

        var lots = await GetAsync<List<VillageLot>>($"/api/villages/{village.Id}/lots");
        Assert.That(lots, Has.Count.EqualTo(1));

        var response = await Client.GetAsync($"/api/villages/{village.Id}/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.That(stats!["totalLots"], Is.EqualTo(1));
    }

    [Test]
    public async Task Village_Search_ByLayout()
    {
        await PostAsync<VirtualVillage>("/api/villages", MakeVillage());
        var results = await GetAsync<List<VirtualVillage>>("/api/villages?layout=Grid");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 2. Walkthroughs ──

    [Test]
    public async Task Walkthrough_GenerateAndRetrieve()
    {
        var homeModelId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/walkthroughs/generate?homeModelId={homeModelId}&userId={userId}", null);
        response.EnsureSuccessStatusCode();
        var walkthrough = await response.Content.ReadFromJsonAsync<HomeWalkthrough>();

        Assert.That(walkthrough!.Id, Is.Not.EqualTo(Guid.Empty));

        var retrieved = await GetAsync<HomeWalkthrough>($"/api/walkthroughs/{walkthrough.Id}");
        Assert.That(retrieved.HomeModelId, Is.EqualTo(homeModelId));
    }

    [Test]
    public async Task Walkthrough_GetByModel_ReturnsWalkthroughs()
    {
        var homeModelId = Guid.NewGuid();
        await Client.PostAsync($"/api/walkthroughs/generate?homeModelId={homeModelId}&userId={Guid.NewGuid()}", null);

        var results = await GetAsync<List<HomeWalkthrough>>($"/api/walkthroughs/by-model/{homeModelId}");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 3. Design Editor ──

    [Test]
    public async Task DesignEditor_ApplyAndUndoEdit()
    {
        var edit = MakeDesignEdit();
        var created = await PostAsync<DesignEdit>("/api/design-editor/edits", edit);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var history = await GetAsync<List<DesignEdit>>(
            $"/api/design-editor/placements/{edit.SitePlacementId}/history");
        Assert.That(history, Has.Count.EqualTo(1));

        // Undo
        var undoResponse = await Client.PostAsync(
            $"/api/design-editor/placements/{edit.SitePlacementId}/undo", null);
        undoResponse.EnsureSuccessStatusCode();

        var afterUndo = await GetAsync<List<DesignEdit>>(
            $"/api/design-editor/placements/{edit.SitePlacementId}/history");
        Assert.That(afterUndo, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task DesignEditor_Reset_ClearsAllEdits()
    {
        var placementId = Guid.NewGuid();
        await PostAsync<DesignEdit>("/api/design-editor/edits",
            MakeDesignEdit() with { SitePlacementId = placementId });
        await PostAsync<DesignEdit>("/api/design-editor/edits",
            MakeDesignEdit() with { SitePlacementId = placementId, Operation = EditOperationType.Rotate });

        var response = await Client.DeleteAsync(
            $"/api/design-editor/placements/{placementId}/reset");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.That(body!["removedEdits"], Is.EqualTo(2));
    }

    // ── 4. Video-to-3D ──

    [Test]
    public async Task VideoToModel_UploadAndAdvance()
    {
        var userId = Guid.NewGuid();
        var response = await Client.PostAsync(
            $"/api/video-to-model/upload?fileName=house.mp4&fileSizeBytes=50000000&durationSeconds=120&userId={userId}", null);
        response.EnsureSuccessStatusCode();
        var job = await response.Content.ReadFromJsonAsync<VideoToModelJob>();

        Assert.That(job!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(job.Status, Is.EqualTo(VideoProcessingStatus.Queued));

        // Advance through stages
        var advance1 = await Client.PostAsync($"/api/video-to-model/jobs/{job.Id}/advance", null);
        advance1.EnsureSuccessStatusCode();
        var advanced = await advance1.Content.ReadFromJsonAsync<VideoToModelJob>();
        Assert.That(advanced!.Status, Is.EqualTo(VideoProcessingStatus.Analysing));

        // Check by user
        var byUser = await GetAsync<List<VideoToModelJob>>($"/api/video-to-model/jobs/by-user/{userId}");
        Assert.That(byUser, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 5. Content ──

    [Test]
    public async Task Content_CreatePostAndPublishAndRate()
    {
        var post = MakeContentPost();
        var created = await PostAsync<ContentPost>("/api/content/posts", post);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Status, Is.EqualTo(ContentPostStatus.Draft));

        // Publish
        var publishResponse = await Client.PostAsync($"/api/content/posts/{created.Id}/publish", null);
        publishResponse.EnsureSuccessStatusCode();
        var published = await publishResponse.Content.ReadFromJsonAsync<ContentPost>();
        Assert.That(published!.Status, Is.EqualTo(ContentPostStatus.Published));

        // Rate
        var rating = new ContentRating(Guid.Empty, created.Id, 5, "Amazing home!", Guid.NewGuid(), default);
        var ratingResponse = await Client.PostAsJsonAsync($"/api/content/posts/{created.Id}/ratings", rating);
        ratingResponse.EnsureSuccessStatusCode();

        var ratings = await GetAsync<List<ContentRating>>($"/api/content/posts/{created.Id}/ratings");
        Assert.That(ratings, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Content_SearchByStatus()
    {
        var post = MakeContentPost();
        var created = await PostAsync<ContentPost>("/api/content/posts", post);
        await Client.PostAsync($"/api/content/posts/{created.Id}/publish", null);

        var results = await GetAsync<List<ContentPost>>("/api/content/posts?status=Published");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── Factory Methods ──

    private static VirtualVillage MakeVillage() => new(
        Guid.Empty, "Sunrise Gardens", "A beautiful community",
        VillageLayoutType.Grid, 50, -33.87, 151.21, Guid.NewGuid(), default);

    private static VillageLot MakeLot(Guid villageId) => new(
        Guid.Empty, villageId, 1, 0.0, 0.0, 15.0, 30.0, VillageLotStatus.Vacant, null);

    private static DesignEdit MakeDesignEdit() => new(
        Guid.Empty, Guid.NewGuid(), EditOperationType.Move, "Position",
        "0,0", "5,5", Guid.NewGuid(), default);

    private static ContentPost MakeContentPost() => new(
        Guid.Empty, "Our Dream Home", "We built our dream home using Terranes",
        Guid.NewGuid(), null, new List<string> { "https://images.example.com/front.jpg" },
        ContentPostStatus.Draft, 0.0, 0, Guid.NewGuid(), default);
}
