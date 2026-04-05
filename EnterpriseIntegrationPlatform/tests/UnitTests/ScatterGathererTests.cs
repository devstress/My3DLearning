using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ScatterGathererTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<ScatterGatherer<string, string>> _logger = null!;

    private ScatterGatherer<string, string> CreateSut(int timeoutMs = 5_000, int maxRecipients = 50)
    {
        var options = Options.Create(new ScatterGatherOptions
        {
            TimeoutMs = timeoutMs,
            MaxRecipients = maxRecipients,
        });

        return new ScatterGatherer<string, string>(_producer, options, _logger);
    }

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<ScatterGatherer<string, string>>>();
    }

    // ── Constructor guards ───────────────────────────────────────────

    [Test]
    public void Ctor_NullProducer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScatterGatherer<string, string>(
                null!,
                Options.Create(new ScatterGatherOptions()),
                _logger));
    }

    [Test]
    public void Ctor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScatterGatherer<string, string>(
                _producer,
                null!,
                _logger));
    }

    [Test]
    public void Ctor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScatterGatherer<string, string>(
                _producer,
                Options.Create(new ScatterGatherOptions()),
                null!));
    }

    // ── ScatterGatherAsync ───────────────────────────────────────────

    [Test]
    public void ScatterGatherAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ScatterGatherAsync(null!));
    }

    [Test]
    public async Task ScatterGatherAsync_EmptyRecipients_ReturnsEmptyResultNotTimedOut()
    {
        var sut = CreateSut();
        var request = new ScatterRequest<string>(Guid.NewGuid(), "payload", []);

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.Responses, Is.Empty);
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.CorrelationId, Is.EqualTo(request.CorrelationId));
    }

    [Test]
    public async Task ScatterGatherAsync_NullRecipients_ReturnsEmptyResultNotTimedOut()
    {
        var sut = CreateSut();
        var request = new ScatterRequest<string>(Guid.NewGuid(), "payload", null!);

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.Responses, Is.Empty);
        Assert.That(result.TimedOut, Is.False);
    }

    [Test]
    public void ScatterGatherAsync_ExceedsMaxRecipients_ThrowsArgumentException()
    {
        var sut = CreateSut(maxRecipients: 2);
        var request = new ScatterRequest<string>(
            Guid.NewGuid(),
            "payload",
            ["topic-a", "topic-b", "topic-c"]);

        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ScatterGatherAsync(request));

        Assert.That(ex!.Message, Does.Contain("3"));
        Assert.That(ex.Message, Does.Contain("2"));
    }

    [Test]
    public void ScatterGatherAsync_DuplicateCorrelationId_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(timeoutMs: 60_000);
        var correlationId = Guid.NewGuid();

        // First request: block the producer so the operation stays active
        _producer.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource().Task); // never completes

        var cts = new CancellationTokenSource();
        var request1 = new ScatterRequest<string>(correlationId, "p1", ["topic-a"]);
        var firstTask = sut.ScatterGatherAsync(request1, cts.Token);

        // Second request with same correlationId
        var request2 = new ScatterRequest<string>(correlationId, "p2", ["topic-b"]);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ScatterGatherAsync(request2));

        cts.Cancel();
    }

    [Test]
    public async Task ScatterGatherAsync_AllRecipientsRespond_ReturnsAllResponsesNotTimedOut()
    {
        var sut = CreateSut(timeoutMs: 5_000);
        var correlationId = Guid.NewGuid();
        var recipients = new[] { "topic-a", "topic-b" };
        var request = new ScatterRequest<string>(correlationId, "payload", recipients);

        // When scatter publishes, submit responses immediately
        _producer
            .When(p => p.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                // Submit both responses after scatter completes
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await sut.SubmitResponseAsync(correlationId,
                        new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));
                    await sut.SubmitResponseAsync(correlationId,
                        new GatherResponse<string>("topic-b", "resp-b", DateTimeOffset.UtcNow, true, null));
                });
            });

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.Responses, Has.Count.EqualTo(2));
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public async Task ScatterGatherAsync_PublishesToAllRecipients_VerifiesPublishCalls()
    {
        var sut = CreateSut(timeoutMs: 200);
        var correlationId = Guid.NewGuid();
        var recipients = new[] { "topic-a", "topic-b", "topic-c" };
        var request = new ScatterRequest<string>(correlationId, "payload", recipients);

        _producer.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await sut.ScatterGatherAsync(request);

        await _producer.Received(3)
            .PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        foreach (var topic in recipients)
        {
            await _producer.Received(1)
                .PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), topic, Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task ScatterGatherAsync_Timeout_ReturnsPartialResultsWithTimedOutTrue()
    {
        var sut = CreateSut(timeoutMs: 200);
        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(correlationId, "payload", ["topic-a", "topic-b"]);

        // Submit only one response (on the first publish only)
        var submitted = 0;
        _producer
            .When(p => p.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (Interlocked.Increment(ref submitted) == 1)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(50);
                        await sut.SubmitResponseAsync(correlationId,
                            new GatherResponse<string>("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null));
                    });
                }
            });

        var result = await sut.ScatterGatherAsync(request);

        Assert.That(result.Responses, Has.Count.EqualTo(1));
        Assert.That(result.TimedOut, Is.True);
    }

    // ── SubmitResponseAsync ──────────────────────────────────────────

    [Test]
    public void SubmitResponseAsync_NullResponse_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.SubmitResponseAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public async Task SubmitResponseAsync_UnknownCorrelationId_ReturnsFalse()
    {
        var sut = CreateSut();
        var response = new GatherResponse<string>("topic-a", "resp", DateTimeOffset.UtcNow, true, null);

        var accepted = await sut.SubmitResponseAsync(Guid.NewGuid(), response);

        Assert.That(accepted, Is.False);
    }

    // ── ScatterGatherResult ──────────────────────────────────────────

    [Test]
    public void ScatterGatherResult_RecordProperties_RetainValues()
    {
        var id = Guid.NewGuid();
        var responses = new List<GatherResponse<string>>
        {
            new("topic-a", "resp-a", DateTimeOffset.UtcNow, true, null),
        };
        var duration = TimeSpan.FromMilliseconds(123);

        var result = new ScatterGatherResult<string>(id, responses, TimedOut: true, duration);

        Assert.That(result.CorrelationId, Is.EqualTo(id));
        Assert.That(result.Responses, Has.Count.EqualTo(1));
        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Duration, Is.EqualTo(duration));
    }

    // ── GatherResponse ───────────────────────────────────────────────

    [Test]
    public void GatherResponse_RecordProperties_RetainValues()
    {
        var now = DateTimeOffset.UtcNow;
        var response = new GatherResponse<string>("topic-a", "resp", now, false, "error msg");

        Assert.That(response.Recipient, Is.EqualTo("topic-a"));
        Assert.That(response.Payload, Is.EqualTo("resp"));
        Assert.That(response.ReceivedAt, Is.EqualTo(now));
        Assert.That(response.IsSuccess, Is.False);
        Assert.That(response.ErrorMessage, Is.EqualTo("error msg"));
    }

    // ── ScatterRequest ───────────────────────────────────────────────

    [Test]
    public void ScatterRequest_RecordProperties_RetainValues()
    {
        var id = Guid.NewGuid();
        var recipients = new[] { "a", "b" };
        var request = new ScatterRequest<string>(id, "payload", recipients);

        Assert.That(request.CorrelationId, Is.EqualTo(id));
        Assert.That(request.Payload, Is.EqualTo("payload"));
        Assert.That(request.Recipients, Is.EquivalentTo(recipients));
    }
}

[TestFixture]
public class ScatterGatherOptionsTests
{
    [Test]
    public void TimeoutMs_Default_Is30000()
    {
        var options = new ScatterGatherOptions();
        Assert.That(options.TimeoutMs, Is.EqualTo(30_000));
    }

    [Test]
    public void MaxRecipients_Default_Is50()
    {
        var options = new ScatterGatherOptions();
        Assert.That(options.MaxRecipients, Is.EqualTo(50));
    }

    [Test]
    public void Properties_WhenSet_RetainValues()
    {
        var options = new ScatterGatherOptions
        {
            TimeoutMs = 5_000,
            MaxRecipients = 10,
        };

        Assert.That(options.TimeoutMs, Is.EqualTo(5_000));
        Assert.That(options.MaxRecipients, Is.EqualTo(10));
    }
}

[TestFixture]
public class ScatterGatherServiceExtensionsTests
{
    [Test]
    public void AddScatterGather_NullServices_ThrowsArgumentNullException()
    {
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            ScatterGatherServiceExtensions.AddScatterGather<string, string>(null!, config));
    }

    [Test]
    public void AddScatterGather_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddScatterGather<string, string>(null!));
    }

    [Test]
    public void AddScatterGather_ValidArgs_RegistersServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ScatterGather:TimeoutMs"] = "10000",
                ["ScatterGather:MaxRecipients"] = "25",
            })
            .Build();

        services.AddSingleton(Substitute.For<IMessageBrokerProducer>());
        services.AddLogging();
        services.AddScatterGather<string, string>(config);

        var sp = services.BuildServiceProvider();
        var instance = sp.GetService<IScatterGatherer<string, string>>();

        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ScatterGatherer<string, string>>());
    }
}
