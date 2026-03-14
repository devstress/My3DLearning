using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Integration;

/// <summary>
/// End-to-end observability tests that exercise the full OpenClaw HTTP API
/// backed by real Grafana Loki storage via Testcontainers.
/// <para>
/// These tests verify the complete "where is my message?" flow through the
/// OpenClaw web application — recording lifecycle events, querying via the
/// REST API, and using AI-powered analysis to confirm message delivery status.
/// </para>
/// <para>
/// Tests are skipped when Docker is not available.
/// </para>
/// </summary>
public class OpenClawObservabilityEndToEndTests : IAsyncLifetime
{
    private IContainer? _lokiContainer;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private bool _dockerAvailable;
    private string _lokiBaseUrl = "";
    private readonly ITraceAnalyzer _traceAnalyzer = Substitute.For<ITraceAnalyzer>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task InitializeAsync()
    {
        try
        {
            _lokiContainer = new ContainerBuilder()
                .WithImage("grafana/loki:3.4.2")
                .WithPortBinding(3100, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(3100).ForPath("/ready")))
                .Build();

            await _lokiContainer.StartAsync();
            _dockerAvailable = true;

            var host = _lokiContainer.Hostname;
            var port = _lokiContainer.GetMappedPublicPort(3100);
            _lokiBaseUrl = $"http://{host}:{port}";

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("Loki:BaseAddress", _lokiBaseUrl);

                    builder.ConfigureServices(services =>
                    {
                        // Replace ITraceAnalyzer with a mock to simulate AI responses
                        var existing = services.FirstOrDefault(
                            d => d.ServiceType == typeof(ITraceAnalyzer));
                        if (existing is not null) services.Remove(existing);
                        services.AddSingleton(_traceAnalyzer);
                    });
                });

            _client = _factory.CreateClient();
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        if (_lokiContainer is not null)
        {
            await _lokiContainer.DisposeAsync();
        }
    }

    private bool SkipIfNoDocker() => !_dockerAvailable;

    /// <summary>Wait for Loki's eventual-consistency write path.</summary>
    private static async Task WaitForLokiIndex() => await Task.Delay(2000);

    private static IntegrationEnvelope<string> CreateEnvelope(string messageType = "OrderShipment")
    {
        return IntegrationEnvelope<string>.Create(
            payload: "test-payload",
            source: "Gateway",
            messageType: messageType);
    }

    [Fact]
    public async Task OpenClaw_InspectBusinessKey_AI_FindsDeliveredMessage()
    {
        if (SkipIfNoDocker()) return;

        // ── Arrange: record a full lifecycle via the app's DI ─────────────────
        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-openclaw-{Guid.NewGuid():N}";

        using var scope = _factory!.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<MessageLifecycleRecorder>();

        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageTransformation, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 123.4, businessKey);

        await WaitForLokiIndex();

        // ── Arrange: configure AI mock ───────────────────────────────────────
        _traceAnalyzer
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                return Task.FromResult(
                    $"The message for {businessKey} has been successfully delivered. " +
                    "It passed through Ingestion, Routing, Transformation, and Delivery stages.");
            });

        // ── Act: call OpenClaw API to ask "where is my message?" ─────────────
        var response = await _client!.GetAsync($"/api/inspect/business/{businessKey}");

        // ── Assert: HTTP response is successful ──────────────────────────────
        response.IsSuccessStatusCode.Should().BeTrue(
            $"OpenClaw API should return success for business key '{businessKey}'");

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<InspectionResultDto>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.Found.Should().BeTrue("the message lifecycle was recorded in Loki");
        result.LatestStatus.Should().Be(DeliveryStatus.Delivered);
        result.LatestStage.Should().Be(MessageTracer.StageDelivery);
        result.Events.Should().HaveCount(4);
        result.OllamaAvailable.Should().BeTrue();
        result.Summary.Should().Contain("delivered");

        // Verify AI was called with the correct correlation ID
        await _traceAnalyzer.Received(1)
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenClaw_InspectCorrelationId_AI_FindsDeliveredMessage()
    {
        if (SkipIfNoDocker()) return;

        // ── Arrange: record lifecycle via the app's DI ───────────────────────
        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-openclaw-corr-{Guid.NewGuid():N}";

        using var scope = _factory!.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<MessageLifecycleRecorder>();

        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 75.0, businessKey);

        await WaitForLokiIndex();

        // ── Arrange: configure AI mock ───────────────────────────────────────
        _traceAnalyzer
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message delivered successfully after passing through Routing.");

        // ── Act: call OpenClaw API by correlation ID ─────────────────────────
        var response = await _client!.GetAsync($"/api/inspect/correlation/{correlationId}");

        // ── Assert ───────────────────────────────────────────────────────────
        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<InspectionResultDto>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.Found.Should().BeTrue();
        result.LatestStatus.Should().Be(DeliveryStatus.Delivered);
        result.LatestStage.Should().Be(MessageTracer.StageDelivery);
        result.Events.Should().HaveCount(3);
        result.Summary.Should().Contain("delivered");
    }

    /// <summary>
    /// DTO for deserializing the OpenClaw API response. Mirrors
    /// <see cref="InspectionResult"/> without requiring a direct reference.
    /// </summary>
    private sealed class InspectionResultDto
    {
        public string Query { get; set; } = "";
        public bool Found { get; set; }
        public string Summary { get; set; } = "";
        public bool OllamaAvailable { get; set; }
        public List<MessageEventDto> Events { get; set; } = [];
        public string? LatestStage { get; set; }
        public DeliveryStatus? LatestStatus { get; set; }
    }

    private sealed class MessageEventDto
    {
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public string MessageType { get; set; } = "";
        public string Source { get; set; } = "";
        public string Stage { get; set; } = "";
        public DeliveryStatus Status { get; set; }
        public string? BusinessKey { get; set; }
        public string? Details { get; set; }
        public DateTimeOffset RecordedAt { get; set; }
    }
}
