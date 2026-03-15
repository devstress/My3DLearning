using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Idempotent Receiver pattern.
/// Ensures a message is processed at most once, even if delivered multiple times.
/// BizTalk equivalent: Idempotent processing via BAM or custom pipeline components.
/// EIP: Idempotent Receiver (p. 528)
/// </summary>
public class IdempotentReceiverTests
{
    private record PaymentPayload(string PaymentId, decimal Amount);

    [Fact]
    public async Task Processes_FirstDelivery()
    {
        var receiver = new IdempotentReceiver<PaymentPayload>();
        var processed = new List<Guid>();

        var envelope = IntegrationEnvelope<PaymentPayload>.Create(
            new PaymentPayload("PAY-001", 100), "PaymentGateway", "PaymentReceived");

        var result = await receiver.TryProcessAsync(
            envelope,
            async (env, ct) => processed.Add(env.MessageId));

        result.Should().BeTrue();
        processed.Should().HaveCount(1);
    }

    [Fact]
    public async Task Rejects_DuplicateDelivery()
    {
        var receiver = new IdempotentReceiver<PaymentPayload>();
        var processCount = 0;

        var envelope = IntegrationEnvelope<PaymentPayload>.Create(
            new PaymentPayload("PAY-001", 100), "PaymentGateway", "PaymentReceived");

        // First delivery — processed
        await receiver.TryProcessAsync(envelope,
            async (env, ct) => processCount++);

        // Duplicate delivery — rejected
        var duplicate = await receiver.TryProcessAsync(envelope,
            async (env, ct) => processCount++);

        duplicate.Should().BeFalse();
        processCount.Should().Be(1); // Only processed once
    }

    [Fact]
    public async Task Tracks_DifferentMessages_Independently()
    {
        var receiver = new IdempotentReceiver<PaymentPayload>();

        var pay1 = IntegrationEnvelope<PaymentPayload>.Create(
            new PaymentPayload("PAY-001", 100), "PG", "PaymentReceived");
        var pay2 = IntegrationEnvelope<PaymentPayload>.Create(
            new PaymentPayload("PAY-002", 200), "PG", "PaymentReceived");

        var r1 = await receiver.TryProcessAsync(pay1, async (e, c) => { });
        var r2 = await receiver.TryProcessAsync(pay2, async (e, c) => { });

        r1.Should().BeTrue();
        r2.Should().BeTrue();
        receiver.ProcessedCount.Should().Be(2);
    }
}
