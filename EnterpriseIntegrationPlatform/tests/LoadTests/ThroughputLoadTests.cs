using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.DeadLetter;
using EnterpriseIntegrationPlatform.Processing.Replay;
using EnterpriseIntegrationPlatform.Processing.Retry;
using EnterpriseIntegrationPlatform.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Load;

/// <summary>
/// In-process throughput load tests that measure platform component performance
/// under concurrent load. These tests use real implementations with in-memory state
/// (no external infrastructure). Each test measures actual elapsed time and throughput.
///
/// Target thresholds are intentionally generous (10× what a production deployment achieves)
/// to avoid flaky failures in CI — the goal is to catch catastrophic regressions, not to
/// enforce specific production SLAs.
/// </summary>
public class ThroughputLoadTests
{
    private static IntegrationEnvelope<string> BuildEnvelope(int index) =>
        IntegrationEnvelope<string>.Create(
            $"payload-{index}",
            "LoadTestService",
            "LoadTestEvent");

    /// <summary>
    /// Publishes 1,000 messages concurrently to an in-memory DLQ publisher and asserts
    /// that all messages are processed within 5 seconds.
    /// </summary>
    [Fact]
    public async Task DeadLetterPublisher_1000ConcurrentPublishes_CompletesWithin5Seconds()
    {
        const int messageCount = 1000;
        var producer = Substitute.For<IMessageBrokerProducer>();
        var publisher = new DeadLetterPublisher<string>(
            producer,
            Options.Create(new DeadLetterOptions { DeadLetterTopic = "dlq" }));

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, messageCount).Select(i =>
            publisher.PublishAsync(
                BuildEnvelope(i),
                DeadLetterReason.MaxRetriesExceeded,
                "load-test",
                3,
                CancellationToken.None));

        await Task.WhenAll(tasks);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            $"expected {messageCount} DLQ publishes to complete within 5 seconds, took {sw.Elapsed.TotalSeconds:F2}s");
        await producer.ReceivedWithAnyArgs(messageCount).PublishAsync<DeadLetterEnvelope<string>>(default!, default!, default);
    }

    /// <summary>
    /// Stores and retrieves 500 messages in the in-memory replay store concurrently
    /// and asserts all operations complete within 5 seconds.
    /// </summary>
    [Fact]
    public async Task InMemoryReplayStore_500ConcurrentStores_CompletesWithin5Seconds()
    {
        const int messageCount = 500;
        var store = new InMemoryMessageReplayStore();
        const string topic = "load-test-topic";

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, messageCount).Select(i =>
            store.StoreForReplayAsync(BuildEnvelope(i), topic, CancellationToken.None));

        await Task.WhenAll(tasks);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            $"expected {messageCount} store operations to complete in 5 seconds, took {sw.Elapsed.TotalSeconds:F2}s");

        var retrieved = new List<IntegrationEnvelope<object>>();
        await foreach (var env in store.GetMessagesForReplayAsync(topic, new ReplayFilter(), messageCount, CancellationToken.None))
            retrieved.Add(env);

        retrieved.Should().HaveCount(messageCount);
    }

    /// <summary>
    /// Runs 200 retry policy executions concurrently (zero delay configured) and asserts
    /// all complete within 5 seconds.
    /// </summary>
    [Fact]
    public async Task ExponentialBackoffRetryPolicy_200ConcurrentSucceedingOperations_CompletesWithin5Seconds()
    {
        const int operationCount = 200;
        var policy = new ExponentialBackoffRetryPolicy(
            Options.Create(new RetryOptions { MaxAttempts = 1, InitialDelayMs = 0, UseJitter = false }),
            NullLogger<ExponentialBackoffRetryPolicy>.Instance);

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount).Select(i =>
            policy.ExecuteAsync<int>(ct => Task.FromResult(i), CancellationToken.None));

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            $"expected {operationCount} retry executions to complete in 5 seconds, took {sw.Elapsed.TotalSeconds:F2}s");
        results.Should().OnlyContain(r => r.IsSucceeded);
    }

    /// <summary>
    /// Validates 10,000 payloads against the PayloadSizeGuard concurrently and asserts
    /// completion within 2 seconds.
    /// </summary>
    [Fact]
    public void PayloadSizeGuard_10000ConcurrentValidations_CompletesWithin2Seconds()
    {
        const int validationCount = 10_000;
        var guard = new PayloadSizeGuard(
            Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 1_048_576 }));

        var payload = new string('x', 1024); // 1 KB payload

        var sw = Stopwatch.StartNew();

        Parallel.For(0, validationCount, _ => guard.Enforce(payload));
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            $"expected {validationCount} payload validations to complete in 2 seconds, took {sw.Elapsed.TotalSeconds:F2}s");
    }
}
