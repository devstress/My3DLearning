using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class MessageExpirationCheckerTests
{
    private IDeadLetterPublisher<string> _dlqPublisher = null!;
    private ILogger<MessageExpirationChecker<string>> _logger = null!;
    private FakeTimeProvider _timeProvider = null!;
    private MessageExpirationChecker<string> _checker = null!;

    [SetUp]
    public void SetUp()
    {
        _dlqPublisher = Substitute.For<IDeadLetterPublisher<string>>();
        _logger = Substitute.For<ILogger<MessageExpirationChecker<string>>>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
        _checker = new MessageExpirationChecker<string>(_dlqPublisher, _logger, _timeProvider);
    }

    // ── No expiry ─────────────────────────────────────────────────────────────

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_ReturnsFalse_WhenExpiresAtIsNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        var result = await _checker.CheckAndRouteIfExpiredAsync(envelope);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_DoesNotPublishToDlq_WhenExpiresAtIsNull()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent");

        await _checker.CheckAndRouteIfExpiredAsync(envelope);

        await _dlqPublisher.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<DeadLetterReason>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── Not expired ───────────────────────────────────────────────────────────

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_ReturnsFalse_WhenMessageNotExpired()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddHours(1) };

        var result = await _checker.CheckAndRouteIfExpiredAsync(envelope);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_DoesNotPublishToDlq_WhenMessageNotExpired()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddHours(1) };

        await _checker.CheckAndRouteIfExpiredAsync(envelope);

        await _dlqPublisher.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<DeadLetterReason>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── Expired ───────────────────────────────────────────────────────────────

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_ReturnsTrue_WhenMessageExpired()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddHours(-1) };

        var result = await _checker.CheckAndRouteIfExpiredAsync(envelope);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_PublishesToDlq_WhenMessageExpired()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddHours(-1) };

        await _checker.CheckAndRouteIfExpiredAsync(envelope);

        await _dlqPublisher.Received(1).PublishAsync(
            envelope,
            DeadLetterReason.MessageExpired,
            Arg.Is<string>(s => s.Contains("expired")),
            0,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_ReturnsFalse_WhenExpiresAtEqualsNow()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow() };

        var result = await _checker.CheckAndRouteIfExpiredAsync(envelope);

        Assert.That(result, Is.False);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Test]
    public void CheckAndRouteIfExpiredAsync_ThrowsArgumentNullException_WhenEnvelopeIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _checker.CheckAndRouteIfExpiredAsync(null!));
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_ReturnsTrue_WhenExpiredByOneMillisecond()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddMilliseconds(-1) };

        var result = await _checker.CheckAndRouteIfExpiredAsync(envelope);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CheckAndRouteIfExpiredAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "TestEvent")
            with { ExpiresAt = _timeProvider.GetUtcNow().AddHours(-1) };

        await _checker.CheckAndRouteIfExpiredAsync(envelope, token);

        await _dlqPublisher.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<DeadLetterReason>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            token);
    }
}
