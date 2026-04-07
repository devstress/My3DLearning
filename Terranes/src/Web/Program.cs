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
using Terranes.Quoting;
using Terranes.SitePlacementEngine;
using Web.Components;

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

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
