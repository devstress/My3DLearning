using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class DetourTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private Detour BuildDetour(DetourOptions options) =>
        new(_producer, Options.Create(options), NullLogger<Detour>.Instance);

    private static DetourOptions DefaultOptions(bool enabledAtStartup = false, string? metadataKey = null) =>
        new()
        {
            DetourTopic = "detour.topic",
            OutputTopic = "output.topic",
            EnabledAtStartup = enabledAtStartup,
            DetourMetadataKey = metadataKey,
        };

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        Dictionary<string, string>? metadata = null)
    {
        var json = JsonDocument.Parse("""{"orderId":1}""").RootElement.Clone();
        return new IntegrationEnvelope<JsonElement>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "TestService",
            MessageType = "OrderCreated",
            Payload = json,
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
    }

    // ------------------------------------------------------------------ //
    // Constructor null guards
    // ------------------------------------------------------------------ //

    [Test]
    public void Constructor_NullProducer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Detour(null!, Options.Create(DefaultOptions()), NullLogger<Detour>.Instance));
    }

    [Test]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Detour(_producer, null!, NullLogger<Detour>.Instance));
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Detour(_producer, Options.Create(DefaultOptions()), null!));
    }

    // ------------------------------------------------------------------ //
    // IsEnabled / SetEnabled
    // ------------------------------------------------------------------ //

    [Test]
    public void IsEnabled_DefaultsTo_False()
    {
        var sut = BuildDetour(DefaultOptions(enabledAtStartup: false));

        Assert.That(sut.IsEnabled, Is.False);
    }

    [Test]
    public void IsEnabled_WhenEnabledAtStartup_ReturnsTrue()
    {
        var sut = BuildDetour(DefaultOptions(enabledAtStartup: true));

        Assert.That(sut.IsEnabled, Is.True);
    }

    [Test]
    public void SetEnabled_SetsIsEnabled()
    {
        var sut = BuildDetour(DefaultOptions());

        sut.SetEnabled(true);

        Assert.That(sut.IsEnabled, Is.True);
    }

    [Test]
    public void SetEnabled_ToggleWorks()
    {
        var sut = BuildDetour(DefaultOptions());

        sut.SetEnabled(true);
        Assert.That(sut.IsEnabled, Is.True);

        sut.SetEnabled(false);
        Assert.That(sut.IsEnabled, Is.False);
    }

    // ------------------------------------------------------------------ //
    // RouteAsync — null guard
    // ------------------------------------------------------------------ //

    [Test]
    public void RouteAsync_NullEnvelope_Throws()
    {
        var sut = BuildDetour(DefaultOptions());

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.RouteAsync<string>(null!));
    }

    // ------------------------------------------------------------------ //
    // RouteAsync — global detour toggle
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_WhenEnabled_RoutesToDetourTopic()
    {
        var sut = BuildDetour(DefaultOptions(enabledAtStartup: true));
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("detour.topic"));
    }

    [Test]
    public async Task RouteAsync_WhenDisabled_RoutesToOutputTopic()
    {
        var sut = BuildDetour(DefaultOptions(enabledAtStartup: false));
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.False);
        Assert.That(result.TargetTopic, Is.EqualTo("output.topic"));
    }

    // ------------------------------------------------------------------ //
    // RouteAsync — per-message detour via metadata
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_PerMessageDetour_WhenMetadataKeySet_RoutesToDetourTopic()
    {
        var sut = BuildDetour(DefaultOptions(metadataKey: "x-detour"));
        var envelope = BuildEnvelope(metadata: new Dictionary<string, string>
        {
            ["x-detour"] = "true",
        });

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.True);
        Assert.That(result.TargetTopic, Is.EqualTo("detour.topic"));
    }

    [Test]
    public async Task RouteAsync_PerMessageDetour_WhenMetadataAbsent_RoutesToOutputTopic()
    {
        var sut = BuildDetour(DefaultOptions(metadataKey: "x-detour"));
        var envelope = BuildEnvelope();

        var result = await sut.RouteAsync(envelope);

        Assert.That(result.Detoured, Is.False);
        Assert.That(result.TargetTopic, Is.EqualTo("output.topic"));
    }

    // ------------------------------------------------------------------ //
    // RouteAsync — producer interaction
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_PublishesEnvelopeToTargetTopic()
    {
        var sut = BuildDetour(DefaultOptions(enabledAtStartup: true));
        var envelope = BuildEnvelope();

        await sut.RouteAsync(envelope);

        await _producer.Received(1).PublishAsync(
            envelope,
            "detour.topic",
            Arg.Any<CancellationToken>());
    }
}
