using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.Notifications;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class WebhookServiceTests
{
    private WebhookService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new WebhookService(NullLogger<WebhookService>.Instance);

    // ── 1. Registration ──

    [Test]
    public async Task RegisterAsync_ValidInput_ReturnsActiveRegistration()
    {
        var webhook = await _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", ["journey.started"]);

        Assert.That(webhook.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(webhook.IsActive, Is.True);
        Assert.That(webhook.CallbackUrl, Is.EqualTo("https://partner.com/webhook"));
    }

    [Test]
    public void RegisterAsync_EmptyPartnerId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.RegisterAsync(Guid.Empty, "https://partner.com/webhook", ["test"]));
    }

    [Test]
    public void RegisterAsync_EmptyCallbackUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.RegisterAsync(Guid.NewGuid(), "", ["test"]));
    }

    [Test]
    public void RegisterAsync_InvalidCallbackUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.RegisterAsync(Guid.NewGuid(), "not-a-url", ["test"]));
    }

    [Test]
    public void RegisterAsync_NoTopics_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", []));
    }

    // ── 2. Delivery ──

    [Test]
    public async Task DeliverEventAsync_MatchingWebhook_DeliversSuccessfully()
    {
        var partnerId = Guid.NewGuid();
        await _sut.RegisterAsync(partnerId, "https://partner.com/webhook", ["journey.started"]);

        var evt = new PlatformEvent(Guid.NewGuid(), "journey.started", "{}", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var deliveries = await _sut.DeliverEventAsync(evt);

        Assert.That(deliveries, Has.Count.EqualTo(1));
        Assert.That(deliveries[0].Status, Is.EqualTo(WebhookDeliveryStatus.Delivered));
    }

    [Test]
    public async Task DeliverEventAsync_NonMatchingTopic_ReturnsEmpty()
    {
        await _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", ["quote.completed"]);

        var evt = new PlatformEvent(Guid.NewGuid(), "journey.started", "{}", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var deliveries = await _sut.DeliverEventAsync(evt);

        Assert.That(deliveries, Is.Empty);
    }

    [Test]
    public async Task DeliverEventAsync_DeactivatedWebhook_SkipsDelivery()
    {
        var webhook = await _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", ["test"]);
        await _sut.DeactivateAsync(webhook.Id);

        var evt = new PlatformEvent(Guid.NewGuid(), "test", "{}", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var deliveries = await _sut.DeliverEventAsync(evt);

        Assert.That(deliveries, Is.Empty);
    }

    // ── 3. Lifecycle ──

    [Test]
    public async Task DeactivateAsync_ActiveWebhook_SetsInactive()
    {
        var webhook = await _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", ["test"]);
        var deactivated = await _sut.DeactivateAsync(webhook.Id);

        Assert.That(deactivated.IsActive, Is.False);
    }

    [Test]
    public async Task GetPartnerWebhooksAsync_FiltersByPartner()
    {
        var partnerId = Guid.NewGuid();
        await _sut.RegisterAsync(partnerId, "https://p1.com/wh", ["a"]);
        await _sut.RegisterAsync(partnerId, "https://p1.com/wh2", ["b"]);
        await _sut.RegisterAsync(Guid.NewGuid(), "https://other.com/wh", ["a"]);

        var webhooks = await _sut.GetPartnerWebhooksAsync(partnerId);
        Assert.That(webhooks, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetDeliveryHistoryAsync_ReturnsDeliveries()
    {
        var webhook = await _sut.RegisterAsync(Guid.NewGuid(), "https://partner.com/webhook", ["test"]);
        var evt = new PlatformEvent(Guid.NewGuid(), "test", "{}", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await _sut.DeliverEventAsync(evt);

        var history = await _sut.GetDeliveryHistoryAsync(webhook.Id);
        Assert.That(history, Has.Count.EqualTo(1));
    }

    [Test]
    public void DeactivateAsync_NonExistent_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }
}
