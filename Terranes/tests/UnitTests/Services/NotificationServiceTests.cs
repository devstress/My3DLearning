using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Notifications;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class NotificationServiceTests
{
    private NotificationService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new NotificationService(NullLogger<NotificationService>.Instance);

    // ── 1. Sending Notifications ──

    [Test]
    public async Task SendAsync_ValidInput_ReturnsDeliveredNotification()
    {
        var recipientId = Guid.NewGuid();
        var notification = await _sut.SendAsync(recipientId, NotificationType.QuoteReady, "Quote Ready", "Your quote is ready for review.");

        Assert.That(notification.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Delivered));
        Assert.That(notification.RecipientId, Is.EqualTo(recipientId));
    }

    [Test]
    public async Task SendAsync_WithEntityId_SetsEntityId()
    {
        var entityId = Guid.NewGuid();
        var notification = await _sut.SendAsync(Guid.NewGuid(), NotificationType.ReferralCreated, "Referral", "New referral", entityId);

        Assert.That(notification.EntityId, Is.EqualTo(entityId));
    }

    [Test]
    public void SendAsync_EmptyRecipient_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SendAsync(Guid.Empty, NotificationType.Info, "Test", "Test message"));
    }

    [Test]
    public void SendAsync_EmptyTitle_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "", "Test message"));
    }

    [Test]
    public void SendAsync_EmptyMessage_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "Test", ""));
    }

    // ── 2. Reading & Status ──

    [Test]
    public async Task MarkAsReadAsync_DeliveredNotification_SetsReadStatus()
    {
        var notification = await _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "Test", "Test message");
        var read = await _sut.MarkAsReadAsync(notification.Id);

        Assert.That(read.Status, Is.EqualTo(NotificationStatus.Read));
    }

    [Test]
    public async Task MarkAsReadAsync_AlreadyRead_ReturnsIdempotently()
    {
        var notification = await _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "Test", "Test message");
        await _sut.MarkAsReadAsync(notification.Id);
        var readAgain = await _sut.MarkAsReadAsync(notification.Id);

        Assert.That(readAgain.Status, Is.EqualTo(NotificationStatus.Read));
    }

    [Test]
    public void MarkAsReadAsync_NonExistent_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.MarkAsReadAsync(Guid.NewGuid()));
    }

    // ── 3. Queries ──

    [Test]
    public async Task GetNotificationsForRecipientAsync_ReturnsAll()
    {
        var recipientId = Guid.NewGuid();
        await _sut.SendAsync(recipientId, NotificationType.QuoteReady, "Quote", "Ready");
        await _sut.SendAsync(recipientId, NotificationType.Info, "Info", "Message");
        await _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "Other", "Not this one");

        var notifications = await _sut.GetNotificationsForRecipientAsync(recipientId);
        Assert.That(notifications, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetUnreadAsync_ExcludesReadNotifications()
    {
        var recipientId = Guid.NewGuid();
        var n1 = await _sut.SendAsync(recipientId, NotificationType.QuoteReady, "Quote", "Ready");
        await _sut.SendAsync(recipientId, NotificationType.Info, "Info", "Message");
        await _sut.MarkAsReadAsync(n1.Id);

        var unread = await _sut.GetUnreadAsync(recipientId);
        Assert.That(unread, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetNotificationAsync_ExistingNotification_ReturnsIt()
    {
        var notification = await _sut.SendAsync(Guid.NewGuid(), NotificationType.Info, "Test", "Test");
        var retrieved = await _sut.GetNotificationAsync(notification.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Test"));
    }
}
