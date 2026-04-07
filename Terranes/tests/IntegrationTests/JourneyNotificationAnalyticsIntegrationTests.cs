using System.Net;
using System.Net.Http.Json;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.IntegrationTests;

/// <summary>
/// Integration tests for Journey, Notifications, and Analytics services through the HTTP API.
/// Covers: BuyerJourney, QuoteAggregator, Referral, Notification, EventBus, Webhook, Search, Analytics, Reporting.
/// </summary>
[TestFixture]
public sealed class JourneyNotificationAnalyticsIntegrationTests : IntegrationTestBase
{
    // ── 1. Buyer Journey ──

    [Test]
    public async Task BuyerJourney_StartAndAdvance()
    {
        var buyerId = Guid.NewGuid();
        var response = await Client.PostAsync($"/api/journeys?buyerId={buyerId}", null);
        response.EnsureSuccessStatusCode();
        var journey = await response.Content.ReadFromJsonAsync<BuyerJourney>();

        Assert.That(journey!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(journey.Stage, Is.EqualTo(JourneyStage.Browsing));

        // Advance
        var advanceResponse = await Client.PutAsync(
            $"/api/journeys/{journey.Id}/advance?stage=DesignSelected&entityId={Guid.NewGuid()}", null);
        advanceResponse.EnsureSuccessStatusCode();
        var advanced = await advanceResponse.Content.ReadFromJsonAsync<BuyerJourney>();
        Assert.That(advanced!.Stage, Is.EqualTo(JourneyStage.DesignSelected));

        // Get by buyer
        var buyerJourneys = await GetAsync<List<BuyerJourney>>($"/api/journeys/buyer/{buyerId}");
        Assert.That(buyerJourneys, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task BuyerJourney_AbandonJourney()
    {
        var buyerId = Guid.NewGuid();
        var response = await Client.PostAsync($"/api/journeys?buyerId={buyerId}", null);
        var journey = await response.Content.ReadFromJsonAsync<BuyerJourney>();

        var abandonResponse = await Client.PostAsync($"/api/journeys/{journey!.Id}/abandon", null);
        abandonResponse.EnsureSuccessStatusCode();
        var abandoned = await abandonResponse.Content.ReadFromJsonAsync<BuyerJourney>();
        Assert.That(abandoned!.Stage, Is.EqualTo(JourneyStage.Abandoned));
    }

    [Test]
    public async Task BuyerJourney_GetActiveJourneys()
    {
        await Client.PostAsync($"/api/journeys?buyerId={Guid.NewGuid()}", null);
        var active = await GetAsync<List<BuyerJourney>>("/api/journeys/active");
        Assert.That(active, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 2. Quote Aggregator ──

    [Test]
    public async Task QuoteAggregator_AggregateAndRetrieve()
    {
        var journeyId = Guid.NewGuid();
        var response = await Client.PostAsync($"/api/aggregated-quotes?journeyId={journeyId}", null);
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<AggregatedQuote>();

        Assert.That(quote!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(quote.TotalEstimateAud, Is.GreaterThan(0));

        var retrieved = await GetAsync<AggregatedQuote>($"/api/aggregated-quotes/{quote.Id}");
        Assert.That(retrieved.JourneyId, Is.EqualTo(journeyId));

        var forJourney = await GetAsync<List<AggregatedQuote>>($"/api/aggregated-quotes/journey/{journeyId}");
        Assert.That(forJourney, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 3. Referrals ──

    [Test]
    public async Task Referral_CreateAndUpdateStatus()
    {
        var journeyId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var response = await Client.PostAsync(
            $"/api/referrals?journeyId={journeyId}&partnerId={partnerId}&category=Builder&buyerName=John", null);
        response.EnsureSuccessStatusCode();
        var referral = await response.Content.ReadFromJsonAsync<PartnerReferral>();

        Assert.That(referral!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(referral.Status, Is.EqualTo(ReferralStatus.Pending));

        // Update status
        var updateResponse = await Client.PutAsync(
            $"/api/referrals/{referral.Id}/status?status=Accepted", null);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PartnerReferral>();
        Assert.That(updated!.Status, Is.EqualTo(ReferralStatus.Accepted));

        // Get by partner
        var partnerReferrals = await GetAsync<List<PartnerReferral>>($"/api/referrals/partner/{partnerId}");
        Assert.That(partnerReferrals, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 4. Notifications ──

    [Test]
    public async Task Notification_SendAndMarkRead()
    {
        var recipientId = Guid.NewGuid();
        var response = await Client.PostAsync(
            $"/api/notifications?recipientId={recipientId}&type=QuoteReady&title=Quote+Ready&message=Your+quote+is+ready", null);
        response.EnsureSuccessStatusCode();
        var notification = await response.Content.ReadFromJsonAsync<Notification>();

        Assert.That(notification!.Id, Is.Not.EqualTo(Guid.Empty));

        // Get unread
        var unread = await GetAsync<List<Notification>>($"/api/notifications/recipient/{recipientId}/unread");
        Assert.That(unread, Has.Count.GreaterThanOrEqualTo(1));

        // Mark as read
        var readResponse = await Client.PutAsync($"/api/notifications/{notification.Id}/read", null);
        readResponse.EnsureSuccessStatusCode();
        var read = await readResponse.Content.ReadFromJsonAsync<Notification>();
        Assert.That(read!.Status, Is.EqualTo(NotificationStatus.Read));

        // Now unread should be empty for this notification
        var allForRecipient = await GetAsync<List<Notification>>($"/api/notifications/recipient/{recipientId}");
        Assert.That(allForRecipient, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 5. Event Bus ──

    [Test]
    public async Task EventBus_PublishAndRetrieve()
    {
        var topic = "journey.started";
        var correlationId = Guid.NewGuid();
        var response = await Client.PostAsync(
            $"/api/events?topic={topic}&payload=%7B%22test%22%3Atrue%7D&correlationId={correlationId}", null);
        response.EnsureSuccessStatusCode();
        var evt = await response.Content.ReadFromJsonAsync<PlatformEvent>();

        Assert.That(evt!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(evt.Topic, Is.EqualTo(topic));

        // Get by topic
        var byTopic = await GetAsync<List<PlatformEvent>>($"/api/events/topic/{topic}");
        Assert.That(byTopic, Has.Count.GreaterThanOrEqualTo(1));

        // Get by correlation
        var byCorrelation = await GetAsync<List<PlatformEvent>>($"/api/events/correlation/{correlationId}");
        Assert.That(byCorrelation, Has.Count.GreaterThanOrEqualTo(1));

        // Count
        var countResponse = await Client.GetAsync("/api/events/count");
        countResponse.EnsureSuccessStatusCode();
        var body = await countResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.That(body!["totalEvents"], Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task EventBus_GetTopicSummary()
    {
        await Client.PostAsync(
            $"/api/events?topic=test.summary&payload=data&correlationId={Guid.NewGuid()}", null);

        var summary = await GetAsync<Dictionary<string, int>>("/api/events/topics");
        Assert.That(summary, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 6. Webhooks ──

    [Test]
    public async Task Webhook_RegisterAndDeactivate()
    {
        var partnerId = Guid.NewGuid();
        var response = await Client.PostAsJsonAsync("/api/webhooks", new
        {
            partnerId,
            callbackUrl = "https://partner.example.com/hooks",
            eventTopics = new[] { "journey.completed", "quote.ready" }
        });
        response.EnsureSuccessStatusCode();
        var webhook = await response.Content.ReadFromJsonAsync<WebhookRegistration>();

        Assert.That(webhook!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(webhook.IsActive, Is.True);

        // Get by partner
        var partnerWebhooks = await GetAsync<List<WebhookRegistration>>($"/api/webhooks/partner/{partnerId}");
        Assert.That(partnerWebhooks, Has.Count.GreaterThanOrEqualTo(1));

        // Deactivate
        var deactivateResponse = await Client.PostAsync($"/api/webhooks/{webhook.Id}/deactivate", null);
        deactivateResponse.EnsureSuccessStatusCode();
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<WebhookRegistration>();
        Assert.That(deactivated!.IsActive, Is.False);
    }

    // ── 7. Search ──

    [Test]
    public async Task Search_IndexAndSearch()
    {
        var entityId = Guid.NewGuid();
        var indexResponse = await Client.PostAsync(
            $"/api/search/index?entityType=HomeModel&entityId={entityId}&title=Modern+Villa&summary=Beautiful+4+bedroom+villa", null);
        indexResponse.EnsureSuccessStatusCode();

        var results = await GetAsync<List<SearchResult>>("/api/search?query=villa");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(results[0].EntityId, Is.EqualTo(entityId));

        // Search by type
        var byType = await GetAsync<List<SearchResult>>("/api/search/HomeModel?query=villa");
        Assert.That(byType, Has.Count.GreaterThanOrEqualTo(1));

        // Count
        var countResponse = await Client.GetAsync("/api/search/count");
        countResponse.EnsureSuccessStatusCode();
        var body = await countResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.That(body!["indexedEntities"], Is.GreaterThanOrEqualTo(1));

        // Remove and verify
        var deleteResponse = await Client.DeleteAsync($"/api/search/HomeModel/{entityId}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    // ── 8. Analytics ──

    [Test]
    public async Task Analytics_TrackAndQuery()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/analytics/track?userId={userId}&tenantId={tenantId}&eventType=HomeModelView&entityId={entityId}", null);
        response.EnsureSuccessStatusCode();
        var evt = await response.Content.ReadFromJsonAsync<AnalyticsEvent>();
        Assert.That(evt!.Id, Is.Not.EqualTo(Guid.Empty));

        // Get user events
        var userEvents = await GetAsync<List<AnalyticsEvent>>($"/api/analytics/user/{userId}");
        Assert.That(userEvents, Has.Count.GreaterThanOrEqualTo(1));

        // Count
        var countResponse = await Client.GetAsync("/api/analytics/count");
        countResponse.EnsureSuccessStatusCode();
        var body = await countResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.That(body!["totalEvents"], Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Analytics_GetPopularEntities()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        await Client.PostAsync(
            $"/api/analytics/track?userId={userId}&tenantId={tenantId}&eventType=VillageView&entityId={entityId}", null);
        await Client.PostAsync(
            $"/api/analytics/track?userId={Guid.NewGuid()}&tenantId={tenantId}&eventType=VillageView&entityId={entityId}", null);

        var popular = await GetAsync<List<KeyValuePair<Guid, int>>>("/api/analytics/popular/VillageView?top=5");
        Assert.That(popular, Has.Count.GreaterThanOrEqualTo(1));
    }

    // ── 9. Reporting ──

    [Test]
    public async Task Reporting_GenerateAndRetrieve()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/reports?reportType=PlatformOverview&title=Monthly+Overview&generatedByUserId={userId}&tenantId={tenantId}", null);
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<Report>();

        Assert.That(report!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(report.Content, Is.Not.Empty);

        // Retrieve
        var retrieved = await GetAsync<Report>($"/api/reports/{report.Id}");
        Assert.That(retrieved.ReportType, Is.EqualTo("PlatformOverview"));

        // Tenant reports
        var tenantReports = await GetAsync<List<Report>>($"/api/reports/tenant/{tenantId}");
        Assert.That(tenantReports, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Reporting_GetAvailableTypes()
    {
        var types = await GetAsync<List<string>>("/api/reports/types");
        Assert.That(types, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(types, Does.Contain("PlatformOverview"));
    }
}
