using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Playwright;

/// <summary>
/// Playwright end-to-end tests for the Admin Dashboard Vue 3 SPA.
/// Tests launch Admin.Web in-process and verify the Vue 3 UI renders correctly,
/// sidebar navigation works, and all dashboard pages are accessible.
/// <para>
/// The Admin.Web proxy endpoints forward to Admin.Api (which is not running in test
/// environment), so API-dependent data displays fallback states. These tests verify
/// the frontend structure, navigation, and form rendering — not live API responses.
/// </para>
/// <para>
/// Tests are skipped when Playwright browsers are not installed.
/// Run <c>pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps</c>
/// to install browsers locally.
/// </para>
/// </summary>
[TestFixture]
public class AdminDashboardTests
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebApplicationFactory<AdminWeb.AdminWebMarker>? _factory;
    private HttpClient? _httpClient;
    private string? _baseUrl;
    private bool _browsersAvailable;

    [SetUp]
    public async Task SetUp()
    {
        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });
            _browsersAvailable = true;
        }
        catch (PlaywrightException)
        {
            _browsersAvailable = false;
            return;
        }

        _factory = new WebApplicationFactory<AdminWeb.AdminWebMarker>();
        _httpClient = _factory.CreateClient();
        _baseUrl = _httpClient.BaseAddress!.ToString().TrimEnd('/');
    }

    [TearDown]
    public async Task TearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    private bool SkipIfNoBrowsers() => !_browsersAvailable;

    // ── Dashboard Page Tests ──────────────────────────────────────────────────

    [Test]
    public async Task Dashboard_LoadsSuccessfully_WithTitle()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var title = await page.TitleAsync();
        Assert.That(title, Does.Contain("EIP Admin Dashboard"));
    }

    [Test]
    public async Task Dashboard_HasSidebarNavigation_WithAllSections()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Verify sidebar links exist for all sections
        var dashboardNav = page.Locator("[data-nav='dashboard']");
        await Expect(dashboardNav).ToBeVisibleAsync();

        var throttleNav = page.Locator("[data-nav='throttle']");
        await Expect(throttleNav).ToBeVisibleAsync();

        var dlqNav = page.Locator("[data-nav='dlq']");
        await Expect(dlqNav).ToBeVisibleAsync();

        var drNav = page.Locator("[data-nav='dr']");
        await Expect(drNav).ToBeVisibleAsync();

        var profilingNav = page.Locator("[data-nav='profiling']");
        await Expect(profilingNav).ToBeVisibleAsync();

        var messagesNav = page.Locator("[data-nav='messages']");
        await Expect(messagesNav).ToBeVisibleAsync();

        var ratelimitNav = page.Locator("[data-nav='ratelimit']");
        await Expect(ratelimitNav).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_ShowsPlatformStatusPage_ByDefault()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Dashboard page should be visible by default
        var dashboardPage = page.Locator("#page-dashboard");
        await Expect(dashboardPage).ToBeVisibleAsync();

        // Header should show dashboard title
        var header = page.Locator(".main header h2");
        var headerText = await header.TextContentAsync();
        Assert.That(headerText, Does.Contain("Dashboard"));
    }

    // ── DLQ Page Tests ────────────────────────────────────────────────────────

    [Test]
    public async Task DlqPage_Navigates_AndShowsResubmitForm()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to DLQ page
        await page.ClickAsync("[data-nav='dlq']");

        // Wait for DLQ page to render
        var dlqPage = page.Locator("#page-dlq");
        await Expect(dlqPage).ToBeVisibleAsync();

        // Verify form fields exist
        var correlationInput = page.Locator("#dlq-correlationId");
        await Expect(correlationInput).ToBeVisibleAsync();

        var messageTypeInput = page.Locator("#dlq-messageType");
        await Expect(messageTypeInput).ToBeVisibleAsync();

        // Verify resubmit button exists
        var resubmitBtn = page.Locator("#btn-resubmit-dlq");
        await Expect(resubmitBtn).ToBeVisibleAsync();
        var btnText = await resubmitBtn.TextContentAsync();
        Assert.That(btnText, Does.Contain("Resubmit"));
    }

    // ── Throttle CRUD Page Tests ──────────────────────────────────────────────

    [Test]
    public async Task ThrottlePage_Navigates_AndShowsPolicyTable()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to Throttle page
        await page.ClickAsync("[data-nav='throttle']");

        // Wait for throttle page to render
        var throttlePage = page.Locator("#page-throttle");
        await Expect(throttlePage).ToBeVisibleAsync();

        // Verify policy table exists
        var table = page.Locator("#throttle-table");
        await Expect(table).ToBeVisibleAsync();

        // Verify Add Policy button exists
        var addBtn = page.Locator("#btn-add-throttle");
        await Expect(addBtn).ToBeVisibleAsync();
        var btnText = await addBtn.TextContentAsync();
        Assert.That(btnText, Does.Contain("Add Policy"));
    }

    [Test]
    public async Task ThrottlePage_AddPolicyForm_OpensAndShowsFields()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to Throttle page
        await page.ClickAsync("[data-nav='throttle']");
        await Expect(page.Locator("#page-throttle")).ToBeVisibleAsync();

        // Click Add Policy button
        await page.ClickAsync("#btn-add-throttle");

        // Verify form is visible with expected fields
        var form = page.Locator("#throttle-form");
        await Expect(form).ToBeVisibleAsync();

        var policyIdInput = page.Locator("#throttle-policyId");
        await Expect(policyIdInput).ToBeVisibleAsync();

        var nameInput = page.Locator("#throttle-name");
        await Expect(nameInput).ToBeVisibleAsync();

        var maxMpsInput = page.Locator("#throttle-maxMps");
        await Expect(maxMpsInput).ToBeVisibleAsync();

        // Save button should exist
        var saveBtn = page.Locator("#btn-save-throttle");
        await Expect(saveBtn).ToBeVisibleAsync();
    }

    // ── DR Drills Page Tests ──────────────────────────────────────────────────

    [Test]
    public async Task DrDrillsPage_Navigates_AndShowsDrillForm()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to DR page
        await page.ClickAsync("[data-nav='dr']");

        // Wait for DR page to render
        var drPage = page.Locator("#page-dr");
        await Expect(drPage).ToBeVisibleAsync();

        // Verify drill form fields
        var scenarioId = page.Locator("#dr-scenarioId");
        await Expect(scenarioId).ToBeVisibleAsync();

        var targetRegion = page.Locator("#dr-targetRegion");
        await Expect(targetRegion).ToBeVisibleAsync();

        // Verify Run Drill button
        var runBtn = page.Locator("#btn-run-drill");
        await Expect(runBtn).ToBeVisibleAsync();
        var btnText = await runBtn.TextContentAsync();
        Assert.That(btnText, Does.Contain("Run Drill"));

        // Verify drill history section
        var historyBtn = page.Locator("#btn-load-dr-history");
        await Expect(historyBtn).ToBeVisibleAsync();
    }

    // ── Message Inspector Page Tests ──────────────────────────────────────────

    [Test]
    public async Task MessageInspectorPage_Navigates_AndShowsSearchForm()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to Messages page
        await page.ClickAsync("[data-nav='messages']");

        // Wait for messages page
        var messagesPage = page.Locator("#page-messages");
        await Expect(messagesPage).ToBeVisibleAsync();

        // Verify search input and button
        var searchInput = page.Locator("#message-query");
        await Expect(searchInput).ToBeVisibleAsync();

        var searchBtn = page.Locator("#btn-search-messages");
        await Expect(searchBtn).ToBeVisibleAsync();
    }

    // ── Profiling Page Tests ──────────────────────────────────────────────────

    [Test]
    public async Task ProfilingPage_Navigates_AndShowsSnapshotControls()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to Profiling page
        await page.ClickAsync("[data-nav='profiling']");

        // Wait for profiling page
        var profilingPage = page.Locator("#page-profiling");
        await Expect(profilingPage).ToBeVisibleAsync();

        // Verify capture button
        var captureBtn = page.Locator("#btn-capture-snapshot");
        await Expect(captureBtn).ToBeVisibleAsync();
        var btnText = await captureBtn.TextContentAsync();
        Assert.That(btnText, Does.Contain("Capture Snapshot"));

        // Verify GC button
        var gcBtn = page.Locator("#btn-load-gc");
        await Expect(gcBtn).ToBeVisibleAsync();
    }

    // ── Navigation Tests ──────────────────────────────────────────────────────

    [Test]
    public async Task SidebarNavigation_SwitchesPages_AndUpdatesActiveState()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Dashboard should be active by default
        var dashboardNav = page.Locator("[data-nav='dashboard']");
        await Expect(dashboardNav).ToHaveClassAsync(new Regex("active"));

        // Click throttle nav
        await page.ClickAsync("[data-nav='throttle']");
        var throttleNav = page.Locator("[data-nav='throttle']");
        await Expect(throttleNav).ToHaveClassAsync(new Regex("active"));

        // Dashboard should no longer have active class
        await Expect(dashboardNav).Not.ToHaveClassAsync(new Regex("active"));

        // Throttle page should be visible
        await Expect(page.Locator("#page-throttle")).ToBeVisibleAsync();
        // Dashboard page should not be visible
        await Expect(page.Locator("#page-dashboard")).Not.ToBeVisibleAsync();
    }

    // ── Rate Limiting Page Tests ──────────────────────────────────────────────

    [Test]
    public async Task RateLimitPage_Navigates_AndShowsRateLimitSection()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Navigate to Rate Limiting page
        await page.ClickAsync("[data-nav='ratelimit']");

        // Wait for rate limit page
        var ratelimitPage = page.Locator("#page-ratelimit");
        await Expect(ratelimitPage).ToBeVisibleAsync();

        // Header should update
        var header = page.Locator(".main header h2");
        var headerText = await header.TextContentAsync();
        Assert.That(headerText, Does.Contain("Rate Limiting"));
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
