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
    public async Task HomePage_HasHintText_MentioningPrometheus()
    {
        if (SkipIfNoBrowsers()) return;

        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(_baseUrl!);

        var hint = page.Locator(".hint");
        await Expect(hint).ToBeVisibleAsync();
        var text = await hint.TextContentAsync();
        text.Should().Contain("Prometheus");
        text.Should().Contain("observability");
    }

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
