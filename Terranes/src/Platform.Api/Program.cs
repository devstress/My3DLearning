using Terranes.Analytics;
using Terranes.Compliance;
using Terranes.Immersive3D;
using Terranes.Infrastructure;
using Terranes.Journey;
using Terranes.Land;
using Terranes.Marketplace;
using Terranes.Models3D;
using Terranes.Notifications;
using Terranes.PartnerIntegration;
using Terranes.Platform.Api.Endpoints;
using Terranes.Quoting;
using Terranes.SitePlacementEngine;

var builder = WebApplication.CreateBuilder(args);

// Register all Terranes services
builder.Services.AddModels3D();
builder.Services.AddLand();
builder.Services.AddSitePlacement();
builder.Services.AddQuoting();
builder.Services.AddMarketplace();
builder.Services.AddCompliance();
builder.Services.AddPartnerIntegration();
builder.Services.AddImmersive3D();
builder.Services.AddInfrastructure();
builder.Services.AddJourney();
builder.Services.AddNotifications();
builder.Services.AddAnalytics();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

// Map API endpoints — core services
app.MapHomeModelEndpoints();
app.MapLandBlockEndpoints();
app.MapSitePlacementEndpoints();
app.MapQuotingEndpoints();
app.MapMarketplaceEndpoints();
app.MapComplianceEndpoints();

// Map API endpoints — partner integration
app.MapBuilderEndpoints();
app.MapLandscaperEndpoints();
app.MapFurnitureEndpoints();
app.MapSmartHomeEndpoints();
app.MapSolicitorEndpoints();
app.MapRealEstateAgentEndpoints();

// Map API endpoints — immersive 3D experience
app.MapVirtualVillageEndpoints();
app.MapWalkthroughEndpoints();
app.MapDesignEditorEndpoints();
app.MapVideoToModelEndpoints();
app.MapContentEndpoints();

// Map API endpoints — platform infrastructure
app.MapAuthEndpoints();
app.MapObservabilityEndpoints();
app.MapTenantEndpoints();

// Map API endpoints — buyer journey
app.MapBuyerJourneyEndpoints();
app.MapQuoteAggregatorEndpoints();
app.MapReferralEndpoints();

// Map API endpoints — notifications & events
app.MapNotificationEndpoints();
app.MapEventBusEndpoints();
app.MapWebhookEndpoints();

// Map API endpoints — search & analytics
app.MapSearchEndpoints();
app.MapAnalyticsEndpoints();
app.MapReportingEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health");

app.Run();

/// <summary>
/// Makes the Program class accessible for integration tests.
/// </summary>
public partial class Program;
