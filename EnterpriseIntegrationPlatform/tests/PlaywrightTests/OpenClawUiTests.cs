using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using NUnit.Framework;

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
[TestFixture]
public class OpenClawUiTests 
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebApplicationFactory<OpenClaw.Web.DemoDataSeeder>? _factory;
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
            // Browsers not installed – tests will be skipped
            _browsersAvailable = false;
            return;
        }

        _factory = new WebApplicationFactory<OpenClaw.Web.DemoDataSeeder>();
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

    private bool SkipIfNoBrowsers()
    {
        // Returns true if test should be skipped (browsers not installed)
        return !_browsersAvailable;
    }

    // ── Page structure tests ──────────────────────────────────────────────────

    [Test]
    public async Task HomePage_LoadsSuccessfully_WithTitle()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var title = await page.TitleAsync();
        Assert.That(title, Does.Contain("OpenClaw"));
    }

    [Test]
    public async Task HomePage_HasSearchBox_AndAskButton()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var input = page.Locator("#query");
        await Expect(input).ToBeVisibleAsync();

        var button = page.Locator("#askBtn");
        await Expect(button).ToBeVisibleAsync();
        var buttonText = await button.TextContentAsync();
        Assert.That(buttonText, Does.Contain("Ask"));
    }

    [Test]
    public async Task HomePage_HasHintText_MentioningObservability()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var hint = page.Locator(".hint");
        await Expect(hint).ToBeVisibleAsync();
        var text = await hint.TextContentAsync();
        Assert.That(text, Does.Contain("observability"));
    }

    [Test]
    public async Task HomePage_ShowsOllamaStatusIndicator()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var ollamaStatus = page.Locator("#ollamaStatus");
        await Expect(ollamaStatus).ToBeVisibleAsync();
        // Wait for the health check to complete — the element transitions from
        // "ollama-checking" to either "ollama-up" or "ollama-down".
        await Expect(ollamaStatus).Not.ToHaveClassAsync(new Regex("ollama-checking"));
        var text = await ollamaStatus.TextContentAsync();
        Assert.That(text, Does.Contain("Ollama"));
    }

    // ── Search and query tests ────────────────────────────────────────────────

    [Test]
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
        var notFoundText = await notFound.TextContentAsync();
        Assert.That(notFoundText, Does.Contain("No messages found"));
    }

    [Test]
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

    [Test]
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
        var cardCount = await cards.CountAsync();
        Assert.That(cardCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
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

    [Test]
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

    [Test]
    public async Task ApiEndpoint_InspectBusiness_ReturnsJson()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/api/inspect/business/order-test-01");
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("\"found\""));
        Assert.That(content, Does.Contain("\"query\""));
    }

    [Test]
    public async Task ApiEndpoint_SeededData_ReturnsFound()
    {
        if (SkipIfNoBrowsers()) return;

        // Poll until the demo seeder has completed rather than using a fixed delay.
        HttpResponseMessage response;
        string content;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        do
        {
            response = await _httpClient!.GetAsync("/api/inspect/business/order-02");
            content = await response.Content.ReadAsStringAsync();
            if (content.Contains("\"found\":true"))
                break;
            await Task.Delay(50);
        } while (DateTime.UtcNow < deadline);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        Assert.That(content, Does.Contain("\"found\":true"));
        Assert.That(content, Does.Contain("\"ollamaAvailable\""));
    }

    [Test]
    public async Task ApiEndpoint_OllamaHealth_ReturnsStatus()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/api/health/ollama");
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("\"available\""));
        Assert.That(content, Does.Contain("\"service\""));
    }

    [Test]
    public async Task MetricsEndpoint_IsAvailable()
    {
        if (SkipIfNoBrowsers()) return;

        var response = await _httpClient!.GetAsync("/metrics");
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Empty);
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
