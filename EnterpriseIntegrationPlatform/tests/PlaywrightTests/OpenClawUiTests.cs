using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Playwright;

/// <summary>
/// Playwright end-to-end tests for the OpenClaw web UI.
/// These tests launch the OpenClaw web app in-process and use Playwright
/// to verify the UI renders correctly and responds to user interactions.
/// <para>
/// Tests are skipped when Playwright browsers are not installed.
/// Run <c>pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps</c>
/// to install browsers locally.
/// </para>
/// </summary>
[Collection("Playwright")]
public class OpenClawUiTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _httpClient;
    private string? _baseUrl;
    private bool _browsersAvailable;

    public async Task InitializeAsync()
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
            // Browsers not installed – tests will be skipped
            _browsersAvailable = false;
            return;
        }

        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
        _baseUrl = _httpClient.BaseAddress!.ToString().TrimEnd('/');
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    private bool SkipIfNoBrowsers()
    {
        // Returns true if test should be skipped (browsers not installed)
        return !_browsersAvailable;
    }

    // ── Page structure tests ──────────────────────────────────────────────────

    [Fact]
    public async Task HomePage_LoadsSuccessfully_WithTitle()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var title = await page.TitleAsync();
        title.Should().Contain("OpenClaw");
    }

    [Fact]
    public async Task HomePage_HasSearchBox_AndAskButton()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var input = page.Locator("#query");
        await Expect(input).ToBeVisibleAsync();

        var button = page.Locator("#askBtn");
        await Expect(button).ToBeVisibleAsync();
        (await button.TextContentAsync()).Should().Contain("Ask");
    }

    [Fact]
    public async Task HomePage_HasHintText_MentioningObservability()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var hint = page.Locator(".hint");
        await Expect(hint).ToBeVisibleAsync();
        var text = await hint.TextContentAsync();
        text.Should().Contain("observability");
    }

    [Fact]
    public async Task HomePage_ShowsOllamaStatusIndicator()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var ollamaStatus = page.Locator("#ollamaStatus");
        await Expect(ollamaStatus).ToBeVisibleAsync();
        // In test environment Ollama is not running, so it should show unavailable
        // after the health check completes
        await page.WaitForTimeoutAsync(2000);
        var text = await ollamaStatus.TextContentAsync();
        text.Should().Contain("Ollama");
    }

    // ── Search and query tests ────────────────────────────────────────────────

    [Fact]
    public async Task SearchForUnknownKey_ShowsNotFound()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        await page.FillAsync("#query", "nonexistent-order-xyz");
        await page.ClickAsync("#askBtn");

        // Wait for result to appear
        var resultDiv = page.Locator("#result");
        await Expect(resultDiv).ToBeVisibleAsync();

        var notFound = page.Locator(".not-found");
        await Expect(notFound).ToBeVisibleAsync();
        (await notFound.TextContentAsync()).Should().Contain("No messages found");
    }

    [Fact]
    public async Task SearchForSeededKey_ShowsResults()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Demo data seeder adds order-02 events
        await page.FillAsync("#query", "order-02");
        await page.ClickAsync("#askBtn");

        var resultDiv = page.Locator("#result");
        await Expect(resultDiv).ToBeVisibleAsync();

        // Should show lifecycle timeline with events (seeded data)
        var timeline = page.Locator(".timeline");
        await Expect(timeline).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SearchForSeededShipment_ShowsInFlightStatus()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Demo data seeder adds shipment-123 events (in-flight)
        await page.FillAsync("#query", "shipment-123");
        await page.ClickAsync("#askBtn");

        var resultDiv = page.Locator("#result");
        await Expect(resultDiv).ToBeVisibleAsync();

        // Should have the status card visible
        var cards = page.Locator(".card");
        (await cards.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SearchBox_SupportsEnterKey()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        await page.FillAsync("#query", "nonexistent-key");
        await page.PressAsync("#query", "Enter");

        var resultDiv = page.Locator("#result");
        await Expect(resultDiv).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OllamaUnavailable_ShowsWarningCard_WhenSearchingSeededData()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        // Search for seeded data – Ollama is not running in test env
        await page.FillAsync("#query", "order-02");
        await page.ClickAsync("#askBtn");

        var resultDiv = page.Locator("#result");
        await Expect(resultDiv).ToBeVisibleAsync();

        // Since Ollama is unavailable, the response should have ollamaAvailable=false
        // and the UI should show the ⚠️ Ollama Unavailable card
        var warningCard = page.Locator("h2:has-text('Ollama Unavailable')");
        await Expect(warningCard).ToBeVisibleAsync();
    }

    // ── API endpoint tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ApiEndpoint_InspectBusiness_ReturnsJson()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/api/inspect/business/order-test-01");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"found\"");
        content.Should().Contain("\"query\"");
    }

    [Fact]
    public async Task ApiEndpoint_SeededData_ReturnsFound()
    {
        if (SkipIfNoBrowsers()) return;

        // Wait for demo seeder to run
        await Task.Delay(500);

        var response = await _httpClient!.GetAsync("/api/inspect/business/order-02");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"found\":true");
        content.Should().Contain("\"ollamaAvailable\"");
    }

    [Fact]
    public async Task ApiEndpoint_OllamaHealth_ReturnsStatus()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/api/health/ollama");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"available\"");
        content.Should().Contain("\"service\"");
    }

    [Fact]
    public async Task MetricsEndpoint_IsAvailable()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/metrics");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
