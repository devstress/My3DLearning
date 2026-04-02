using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Throttle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Throttle;

[TestFixture]
public sealed class TokenBucketThrottleTests
{
    private ILogger<TokenBucketThrottle> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<TokenBucketThrottle>>();
    }

    [Test]
    public async Task AcquireAsync_WithAvailableTokens_ReturnsPermitted()
    {
        using var throttle = CreateThrottle(burstCapacity: 10, maxPerSec: 10);
        var envelope = CreateEnvelope("test-payload");

        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.True);
        Assert.That(result.RejectionReason, Is.Null);
    }

    [Test]
    public async Task AcquireAsync_ExhaustsTokens_ThenWaitsForRefill()
    {
        // Burst of 2, so third acquire must wait for refill.
        using var throttle = CreateThrottle(burstCapacity: 2, maxPerSec: 100);
        var envelope = CreateEnvelope("test-payload");

        var r1 = await throttle.AcquireAsync(envelope);
        var r2 = await throttle.AcquireAsync(envelope);

        Assert.That(r1.Permitted, Is.True);
        Assert.That(r2.Permitted, Is.True);
        Assert.That(throttle.AvailableTokens, Is.EqualTo(0));
    }

    [Test]
    public async Task AcquireAsync_RejectOnBackpressure_ReturnsFalseImmediately()
    {
        using var throttle = CreateThrottle(burstCapacity: 1, maxPerSec: 1, rejectOnBackpressure: true);
        var envelope = CreateEnvelope("test-payload");

        // Consume the single token.
        var first = await throttle.AcquireAsync(envelope);
        Assert.That(first.Permitted, Is.True);

        // Next should be rejected immediately (backpressure, not timeout).
        var second = await throttle.AcquireAsync(envelope);
        Assert.That(second.Permitted, Is.False);
        Assert.That(second.RejectionReason, Does.Contain("Backpressure"));
        Assert.That(second.WaitTime, Is.LessThan(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public async Task AcquireAsync_Timeout_ReturnsFalseWithReason()
    {
        // Burst=1, refill=1/sec, maxWait=50ms — the 100ms refill timer won't fire
        // in time, so the second acquire will timeout.
        using var throttle = CreateThrottle(burstCapacity: 1, maxPerSec: 1, maxWaitMs: 50);
        var envelope = CreateEnvelope("test-payload");

        await throttle.AcquireAsync(envelope); // consume the token
        var result = await throttle.AcquireAsync(envelope);

        Assert.That(result.Permitted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("Timeout"));
    }

    [Test]
    public async Task AcquireAsync_Cancellation_ReturnsFalseWithReason()
    {
        // Use rejectOnBackpressure=false so it waits, then cancel before refill.
        // CTS fires at 20ms — well before the 100ms refill timer can replenish.
        using var throttle = CreateThrottle(burstCapacity: 1, maxPerSec: 1, maxWaitMs: 30000);
        var envelope = CreateEnvelope("test-payload");
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await throttle.AcquireAsync(envelope); // consume
        var result = await throttle.AcquireAsync(envelope, cts.Token);

        Assert.That(result.Permitted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("Cancelled"));
    }

    [Test]
    public async Task AcquireAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        using var throttle = CreateThrottle(burstCapacity: 10, maxPerSec: 10);

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await throttle.AcquireAsync<string>(null!, CancellationToken.None));
    }

    [Test]
    public async Task GetMetrics_TracksAcquiredAndRejected()
    {
        using var throttle = CreateThrottle(burstCapacity: 1, maxPerSec: 1, maxWaitMs: 100, rejectOnBackpressure: true);
        var envelope = CreateEnvelope("test-payload");

        await throttle.AcquireAsync(envelope); // acquired
        await throttle.AcquireAsync(envelope); // rejected (backpressure)

        var metrics = throttle.GetMetrics();
        Assert.That(metrics.TotalAcquired, Is.EqualTo(1));
        Assert.That(metrics.TotalRejected, Is.EqualTo(1));
        Assert.That(metrics.BurstCapacity, Is.EqualTo(1));
        Assert.That(metrics.RefillRate, Is.EqualTo(1));
    }

    [Test]
    public void AvailableTokens_ReflectsBurstCapacityAtStart()
    {
        using var throttle = CreateThrottle(burstCapacity: 50, maxPerSec: 100);

        Assert.That(throttle.AvailableTokens, Is.EqualTo(50));
    }

    private TokenBucketThrottle CreateThrottle(
        int burstCapacity = 100,
        int maxPerSec = 100,
        int maxWaitMs = 30000,
        bool rejectOnBackpressure = false)
    {
        var options = Options.Create(new ThrottleOptions
        {
            BurstCapacity = burstCapacity,
            MaxMessagesPerSecond = maxPerSec,
            MaxWaitTime = TimeSpan.FromMilliseconds(maxWaitMs),
            RejectOnBackpressure = rejectOnBackpressure,
        });

        return new TokenBucketThrottle(options, _logger);
    }

    private static IntegrationEnvelope<string> CreateEnvelope(string payload) =>
        IntegrationEnvelope<string>.Create(payload, "test-source", "TestMessage");
}
