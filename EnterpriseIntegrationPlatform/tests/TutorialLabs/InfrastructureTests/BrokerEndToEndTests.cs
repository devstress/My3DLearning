// ============================================================================
// BrokerEndToEndTests – Real E2E tests for all supported message brokers
// ============================================================================
// Each test uses real Aspire containers — no mocks, no stubs, no fakes.
// Verifies full publish → subscribe → consume round-trips through:
//   1. NATS JetStream   (lightweight, cloud-native)
//   2. Apache Kafka      (high-throughput, ordered, long-retention)
//   3. Apache Pulsar     (Key_Shared recipient-keyed distribution)
//   4. PostgreSQL        (SQL-based, ACID, pg_notify)
//
// Requires Docker; tests fail (Assert.Fail) when unavailable.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.InfrastructureTests;

/// <summary>
/// End-to-end integration tests proving every supported broker transports
/// <see cref="IntegrationEnvelope{T}"/> messages through real containers.
/// </summary>
[TestFixture]
public sealed class BrokerEndToEndTests
{
    // ═════════════════════════════════════════════════════════════════════
    //  NATS JetStream
    // ═════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Nats_PublishSubscribeRoundTrip_DeliversPayload()
    {
        var endpoint = AspireFixture.CreateNatsEndpoint("nats-e2e");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("nats-e2e");
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello from NATS E2E!", "nats-test", "Greeting");

        // Subscribe, publish, verify delivery
        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "e2e-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(500); // Let subscription establish
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received);
        Assert.That(consumed, Is.Not.Null, "Message should be delivered through NATS");
        Assert.That(consumed!.Payload, Is.EqualTo("Hello from NATS E2E!"));
        Assert.That(consumed.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(consumed.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(consumed.Source, Is.EqualTo("nats-test"));
        Assert.That(consumed.MessageType, Is.EqualTo("Greeting"));
    }

    [Test]
    public async Task Nats_MultipleMessages_AllDeliveredInOrder()
    {
        var endpoint = AspireFixture.CreateNatsEndpoint("nats-multi");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("nats-multi");
        var payloads = new List<string>();

        await endpoint.SubscribeAsync<string>(topic, "multi-group", env =>
        {
            payloads.Add(env.Payload);
            return Task.CompletedTask;
        });

        await Task.Delay(500);

        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"msg-{i}", "test", "Seq");
            await endpoint.SendAsync(env, topic);
        }

        await endpoint.WaitForConsumedAsync(5, TimeSpan.FromSeconds(15));

        Assert.That(payloads, Has.Count.EqualTo(5));
        for (var i = 0; i < 5; i++)
            Assert.That(payloads[i], Is.EqualTo($"msg-{i}"));
    }

    [Test]
    public async Task Nats_EnvelopeMetadata_PreservedAcrossRoundTrip()
    {
        var endpoint = AspireFixture.CreateNatsEndpoint("nats-meta");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("nats-meta");
        var envelope = IntegrationEnvelope<string>.Create("metadata test", "source-app", "MetaEvent");
        envelope.Metadata["region"] = "us-west-2";
        envelope.Metadata["tenant"] = "acme-corp";

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "meta-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(500);
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received);
        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed!.Metadata["region"], Is.EqualTo("us-west-2"));
        Assert.That(consumed.Metadata["tenant"], Is.EqualTo("acme-corp"));
        Assert.That(consumed.SchemaVersion, Is.EqualTo("1.0"));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Apache Kafka
    // ═════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Kafka_PublishSubscribeRoundTrip_DeliversPayload()
    {
        var endpoint = AspireFixture.CreateKafkaEndpoint("kafka-e2e");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("kafka-e2e");
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello from Kafka E2E!", "kafka-test", "Greeting");

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "e2e-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(1_000); // Kafka consumer join takes longer
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(30));
        Assert.That(consumed, Is.Not.Null, "Message should be delivered through Kafka");
        Assert.That(consumed!.Payload, Is.EqualTo("Hello from Kafka E2E!"));
        Assert.That(consumed.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(consumed.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(consumed.Source, Is.EqualTo("kafka-test"));
        Assert.That(consumed.MessageType, Is.EqualTo("Greeting"));
    }

    [Test]
    public async Task Kafka_MultipleMessages_AllDelivered()
    {
        var endpoint = AspireFixture.CreateKafkaEndpoint("kafka-multi");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("kafka-multi");
        var payloads = new List<string>();

        await endpoint.SubscribeAsync<string>(topic, "multi-group", env =>
        {
            payloads.Add(env.Payload);
            return Task.CompletedTask;
        });

        await Task.Delay(1_000);

        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"kafka-msg-{i}", "test", "Seq");
            await endpoint.SendAsync(env, topic);
        }

        await endpoint.WaitForConsumedAsync(5, TimeSpan.FromSeconds(30));

        Assert.That(payloads, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task Kafka_EnvelopeMetadata_PreservedAcrossRoundTrip()
    {
        var endpoint = AspireFixture.CreateKafkaEndpoint("kafka-meta");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("kafka-meta");
        var envelope = IntegrationEnvelope<string>.Create("kafka metadata test", "source-app", "MetaEvent");
        envelope.Metadata["priority"] = "high";
        envelope.Metadata["environment"] = "staging";

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "meta-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(1_000);
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(30));
        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed!.Metadata["priority"], Is.EqualTo("high"));
        Assert.That(consumed.Metadata["environment"], Is.EqualTo("staging"));
    }

    [Test]
    public async Task Kafka_ProducerCaptures_PublishedMessages()
    {
        var endpoint = AspireFixture.CreateKafkaEndpoint("kafka-capture");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("kafka-capture");
        var envelope = IntegrationEnvelope<string>.Create("Kafka Captured!", "test", "cmd");

        await endpoint.PublishAsync(envelope, topic);

        endpoint.AssertReceivedCount(1);
        endpoint.AssertReceivedOnTopic(topic, 1);

        var msg = endpoint.GetReceived<string>();
        Assert.That(msg.Payload, Is.EqualTo("Kafka Captured!"));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Apache Pulsar
    // ═════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Pulsar_PublishSubscribeRoundTrip_DeliversPayload()
    {
        var endpoint = AspireFixture.CreatePulsarEndpoint("pulsar-e2e");
        await using var _ = endpoint;

        // Pulsar requires persistent:// topic prefix for persistence
        var topic = $"persistent://public/default/{AspireFixture.UniqueTopic("pulsar-e2e")}";
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello from Pulsar E2E!", "pulsar-test", "Greeting");

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "e2e-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(2_000); // Pulsar consumer join may be slower
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(30));
        Assert.That(consumed, Is.Not.Null, "Message should be delivered through Pulsar");
        Assert.That(consumed!.Payload, Is.EqualTo("Hello from Pulsar E2E!"));
        Assert.That(consumed.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(consumed.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(consumed.Source, Is.EqualTo("pulsar-test"));
        Assert.That(consumed.MessageType, Is.EqualTo("Greeting"));
    }

    [Test]
    public async Task Pulsar_MultipleMessages_AllDelivered()
    {
        var endpoint = AspireFixture.CreatePulsarEndpoint("pulsar-multi");
        await using var _ = endpoint;

        var topic = $"persistent://public/default/{AspireFixture.UniqueTopic("pulsar-multi")}";
        var payloads = new List<string>();

        await endpoint.SubscribeAsync<string>(topic, "multi-group", env =>
        {
            payloads.Add(env.Payload);
            return Task.CompletedTask;
        });

        await Task.Delay(2_000);

        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"pulsar-msg-{i}", "test", "Seq");
            await endpoint.SendAsync(env, topic);
        }

        await endpoint.WaitForConsumedAsync(5, TimeSpan.FromSeconds(30));

        Assert.That(payloads, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task Pulsar_EnvelopeMetadata_PreservedAcrossRoundTrip()
    {
        var endpoint = AspireFixture.CreatePulsarEndpoint("pulsar-meta");
        await using var _ = endpoint;

        var topic = $"persistent://public/default/{AspireFixture.UniqueTopic("pulsar-meta")}";
        var envelope = IntegrationEnvelope<string>.Create("pulsar metadata test", "source-app", "MetaEvent");
        envelope.Metadata["datacenter"] = "us-east-1";
        envelope.Metadata["version"] = "2.0";

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "meta-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(2_000);
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(30));
        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed!.Metadata["datacenter"], Is.EqualTo("us-east-1"));
        Assert.That(consumed.Metadata["version"], Is.EqualTo("2.0"));
    }

    [Test]
    public async Task Pulsar_ProducerCaptures_PublishedMessages()
    {
        var endpoint = AspireFixture.CreatePulsarEndpoint("pulsar-capture");
        await using var _ = endpoint;

        var topic = $"persistent://public/default/{AspireFixture.UniqueTopic("pulsar-capture")}";
        var envelope = IntegrationEnvelope<string>.Create("Pulsar Captured!", "test", "cmd");

        await endpoint.PublishAsync(envelope, topic);

        endpoint.AssertReceivedCount(1);
        endpoint.AssertReceivedOnTopic(topic, 1);

        var msg = endpoint.GetReceived<string>();
        Assert.That(msg.Payload, Is.EqualTo("Pulsar Captured!"));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PostgreSQL
    // ═════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Postgres_PublishSubscribeRoundTrip_DeliversPayload()
    {
        var endpoint = AspireFixture.CreatePostgresEndpoint("pg-e2e");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("pg-e2e");
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello from Postgres E2E!", "pg-test", "Greeting");

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "e2e-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(500);
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(15));
        Assert.That(consumed, Is.Not.Null, "Message should be delivered through Postgres");
        Assert.That(consumed!.Payload, Is.EqualTo("Hello from Postgres E2E!"));
        Assert.That(consumed.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(consumed.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(consumed.Source, Is.EqualTo("pg-test"));
        Assert.That(consumed.MessageType, Is.EqualTo("Greeting"));
    }

    [Test]
    public async Task Postgres_MultipleMessages_AllDelivered()
    {
        var endpoint = AspireFixture.CreatePostgresEndpoint("pg-multi");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("pg-multi");
        var payloads = new List<string>();

        await endpoint.SubscribeAsync<string>(topic, "multi-group", env =>
        {
            payloads.Add(env.Payload);
            return Task.CompletedTask;
        });

        await Task.Delay(500);

        for (var i = 0; i < 5; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"pg-msg-{i}", "test", "Seq");
            await endpoint.SendAsync(env, topic);
        }

        await endpoint.WaitForConsumedAsync(5, TimeSpan.FromSeconds(30));

        Assert.That(payloads, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task Postgres_PollConsumer_RetrievesMessages()
    {
        var endpoint = AspireFixture.CreatePostgresEndpoint("pg-poll");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("pg-poll");

        // Publish 3 messages
        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create($"poll-{i}", "test", "PollEvent");
            await endpoint.PublishAsync(env, topic);
        }

        // Poll for messages
        var messages = await endpoint.PollAsync<string>(topic, "poll-group", maxMessages: 10);

        Assert.That(messages.Count, Is.GreaterThanOrEqualTo(3),
            "All 3 messages should be retrievable via poll");
    }

    [Test]
    public async Task Postgres_EnvelopeMetadata_PreservedAcrossRoundTrip()
    {
        var endpoint = AspireFixture.CreatePostgresEndpoint("pg-meta");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("pg-meta");
        var envelope = IntegrationEnvelope<string>.Create("pg metadata test", "source-app", "MetaEvent");
        envelope.Metadata["schema"] = "orders-v3";
        envelope.Metadata["partition-key"] = "customer-42";

        var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
        await endpoint.SubscribeAsync<string>(topic, "meta-group", env =>
        {
            received.TrySetResult(env);
            return Task.CompletedTask;
        });

        await Task.Delay(500);
        await endpoint.SendAsync(envelope, topic);

        var consumed = await WaitForResult(received, TimeSpan.FromSeconds(15));
        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed!.Metadata["schema"], Is.EqualTo("orders-v3"));
        Assert.That(consumed.Metadata["partition-key"], Is.EqualTo("customer-42"));
    }

    [Test]
    public async Task Postgres_ProducerCaptures_PublishedMessages()
    {
        var endpoint = AspireFixture.CreatePostgresEndpoint("pg-capture");
        await using var _ = endpoint;

        var topic = AspireFixture.UniqueTopic("pg-capture");
        var envelope = IntegrationEnvelope<string>.Create("PG Captured!", "test", "cmd");

        await endpoint.PublishAsync(envelope, topic);

        endpoint.AssertReceivedCount(1);
        endpoint.AssertReceivedOnTopic(topic, 1);

        var msg = endpoint.GetReceived<string>();
        Assert.That(msg.Payload, Is.EqualTo("PG Captured!"));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Cross-Broker: Envelope Fidelity (all brokers preserve the same data)
    // ═════════════════════════════════════════════════════════════════════

    [Test]
    public async Task AllBrokers_ComplexEnvelope_PreservesAllFields()
    {
        // Create a complex envelope with every field populated
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = causationId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = "fidelity-test",
            MessageType = "ComplexEvent",
            SchemaVersion = "2.1",
            Priority = MessagePriority.High,
            Payload = "Cross-broker fidelity payload",
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "eu-west-1",
                ["trace-id"] = "abc-123",
            },
            ReplyTo = "reply-topic",
            SequenceNumber = 7,
            TotalCount = 10,
        };

        // Test on NATS
        await VerifyEnvelopeFidelity("nats", envelope, async (ep, topic, env) =>
        {
            var nats = AspireFixture.CreateNatsEndpoint(ep);
            await using var __ = nats;
            var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
            await nats.SubscribeAsync<string>(topic, "fidelity", e =>
            {
                received.TrySetResult(e);
                return Task.CompletedTask;
            });
            await Task.Delay(500);
            await nats.SendAsync(env, topic);
            return await WaitForResult(received);
        });

        // Test on Postgres
        await VerifyEnvelopeFidelity("pg", envelope, async (ep, topic, env) =>
        {
            var pg = AspireFixture.CreatePostgresEndpoint(ep);
            await using var __ = pg;
            var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
            await pg.SubscribeAsync<string>(topic, "fidelity", e =>
            {
                received.TrySetResult(e);
                return Task.CompletedTask;
            });
            await Task.Delay(500);
            await pg.SendAsync(env, topic);
            return await WaitForResult(received, TimeSpan.FromSeconds(15));
        });

        // Test on Kafka
        await VerifyEnvelopeFidelity("kafka", envelope, async (ep, topic, env) =>
        {
            var kafka = AspireFixture.CreateKafkaEndpoint(ep);
            await using var __ = kafka;
            var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
            await kafka.SubscribeAsync<string>(topic, "fidelity", e =>
            {
                received.TrySetResult(e);
                return Task.CompletedTask;
            });
            await Task.Delay(1_000);
            await kafka.SendAsync(env, topic);
            return await WaitForResult(received, TimeSpan.FromSeconds(30));
        });

        // Test on Pulsar
        await VerifyEnvelopeFidelity("pulsar", envelope, async (ep, topic, env) =>
        {
            var pulsar = AspireFixture.CreatePulsarEndpoint(ep);
            await using var __ = pulsar;
            var pulsarTopic = $"persistent://public/default/{topic}";
            var received = new TaskCompletionSource<IntegrationEnvelope<string>>();
            await pulsar.SubscribeAsync<string>(pulsarTopic, "fidelity", e =>
            {
                received.TrySetResult(e);
                return Task.CompletedTask;
            });
            await Task.Delay(2_000);
            await pulsar.SendAsync(env, pulsarTopic);
            return await WaitForResult(received, TimeSpan.FromSeconds(30));
        });
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════

    private static async Task<T?> WaitForResult<T>(
        TaskCompletionSource<T> tcs,
        TimeSpan? timeout = null) where T : class
    {
        var timeoutSpan = timeout ?? TimeSpan.FromSeconds(15);
        return await Task.WhenAny(tcs.Task, Task.Delay(timeoutSpan)) == tcs.Task
            ? tcs.Task.Result
            : null;
    }

    private static async Task VerifyEnvelopeFidelity(
        string brokerName,
        IntegrationEnvelope<string> original,
        Func<string, string, IntegrationEnvelope<string>, Task<IntegrationEnvelope<string>?>> roundTrip)
    {
        var topic = AspireFixture.UniqueTopic($"fidelity-{brokerName}");
        var consumed = await roundTrip($"{brokerName}-fidelity", topic, original);

        Assert.That(consumed, Is.Not.Null,
            $"[{brokerName}] Message should round-trip through broker");
        Assert.That(consumed!.MessageId, Is.EqualTo(original.MessageId),
            $"[{brokerName}] MessageId preserved");
        Assert.That(consumed.CorrelationId, Is.EqualTo(original.CorrelationId),
            $"[{brokerName}] CorrelationId preserved");
        Assert.That(consumed.CausationId, Is.EqualTo(original.CausationId),
            $"[{brokerName}] CausationId preserved");
        Assert.That(consumed.Source, Is.EqualTo(original.Source),
            $"[{brokerName}] Source preserved");
        Assert.That(consumed.MessageType, Is.EqualTo(original.MessageType),
            $"[{brokerName}] MessageType preserved");
        Assert.That(consumed.SchemaVersion, Is.EqualTo(original.SchemaVersion),
            $"[{brokerName}] SchemaVersion preserved");
        Assert.That(consumed.Priority, Is.EqualTo(original.Priority),
            $"[{brokerName}] Priority preserved");
        Assert.That(consumed.Payload, Is.EqualTo(original.Payload),
            $"[{brokerName}] Payload preserved");
        Assert.That(consumed.ReplyTo, Is.EqualTo(original.ReplyTo),
            $"[{brokerName}] ReplyTo preserved");
        Assert.That(consumed.SequenceNumber, Is.EqualTo(original.SequenceNumber),
            $"[{brokerName}] SequenceNumber preserved");
        Assert.That(consumed.TotalCount, Is.EqualTo(original.TotalCount),
            $"[{brokerName}] TotalCount preserved");
        Assert.That(consumed.Metadata["region"], Is.EqualTo(original.Metadata["region"]),
            $"[{brokerName}] Metadata preserved");
    }
}
