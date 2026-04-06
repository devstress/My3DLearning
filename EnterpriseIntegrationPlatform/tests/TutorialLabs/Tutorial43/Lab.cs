// ============================================================================
// Tutorial 43 – Kubernetes Deployment / Configuration Options (Lab)
// ============================================================================
// This lab exercises the configuration and options classes used by the
// Kubernetes deployment: TemporalOptions, PipelineOptions, JwtOptions,
// and DisasterRecoveryOptions.
// ============================================================================

using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using EnterpriseIntegrationPlatform.Security;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial43;

[TestFixture]
public sealed class Lab
{
    // ── TemporalOptions Properties Assignable ───────────────────────────────

    [Test]
    public void TemporalOptions_PropertiesAssignable()
    {
        var opts = new TemporalOptions
        {
            ServerAddress = "temporal.prod:7233",
            Namespace = "production",
            TaskQueue = "order-workflows",
        };

        Assert.That(opts.ServerAddress, Is.EqualTo("temporal.prod:7233"));
        Assert.That(opts.Namespace, Is.EqualTo("production"));
        Assert.That(opts.TaskQueue, Is.EqualTo("order-workflows"));
    }

    // ── PipelineOptions Properties Assignable ───────────────────────────────

    [Test]
    public void PipelineOptions_PropertiesAssignable()
    {
        var opts = new PipelineOptions
        {
            AckSubject = "pipeline.ack",
            NackSubject = "pipeline.nack",
            InboundSubject = "pipeline.inbound",
            NatsUrl = "nats://nats-server:4222",
            ConsumerGroup = "my-group",
        };

        Assert.That(opts.AckSubject, Is.EqualTo("pipeline.ack"));
        Assert.That(opts.NackSubject, Is.EqualTo("pipeline.nack"));
        Assert.That(opts.InboundSubject, Is.EqualTo("pipeline.inbound"));
        Assert.That(opts.NatsUrl, Is.EqualTo("nats://nats-server:4222"));
        Assert.That(opts.ConsumerGroup, Is.EqualTo("my-group"));
    }

    // ── JwtOptions Defaults ─────────────────────────────────────────────────

    [Test]
    public void JwtOptions_Defaults_ValidateLifetimeAndClockSkew()
    {
        var opts = new JwtOptions();

        Assert.That(opts.ValidateLifetime, Is.True);
        Assert.That(opts.ClockSkew, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(opts.Issuer, Is.EqualTo(string.Empty));
        Assert.That(opts.Audience, Is.EqualTo(string.Empty));
        Assert.That(opts.SigningKey, Is.EqualTo(string.Empty));
    }

    // ── DisasterRecoveryOptions Defaults ────────────────────────────────────

    [Test]
    public void DisasterRecoveryOptions_Defaults()
    {
        var opts = new DisasterRecoveryOptions();

        Assert.That(opts.MaxDrillHistorySize, Is.EqualTo(100));
        Assert.That(opts.MaxReplicationLag, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(opts.HealthCheckInterval, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(opts.OfflineThreshold, Is.EqualTo(3));
        Assert.That(opts.PerItemReplicationTime, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
    }

    // ── Options.Create<TemporalOptions> Works Correctly ─────────────────────

    [Test]
    public void OptionsCreate_TemporalOptions_WorksCorrectly()
    {
        var temporal = new TemporalOptions
        {
            ServerAddress = "localhost:7233",
            Namespace = "test-ns",
            TaskQueue = "test-queue",
        };

        var wrapped = Options.Create(temporal);

        Assert.That(wrapped, Is.Not.Null);
        Assert.That(wrapped.Value.ServerAddress, Is.EqualTo("localhost:7233"));
        Assert.That(wrapped.Value.Namespace, Is.EqualTo("test-ns"));
        Assert.That(wrapped.Value.TaskQueue, Is.EqualTo("test-queue"));
    }

    // ── JwtOptions.SectionName Constant ─────────────────────────────────────

    [Test]
    public void JwtOptions_SectionName_IsJwt()
    {
        Assert.That(JwtOptions.SectionName, Is.EqualTo("Jwt"));
        Assert.That(TemporalOptions.SectionName, Is.EqualTo("Temporal"));
        Assert.That(PipelineOptions.SectionName, Is.EqualTo("Pipeline"));
        Assert.That(DisasterRecoveryOptions.SectionName, Is.EqualTo("DisasterRecovery"));
    }

    // ── All Options Classes Are Sealed ──────────────────────────────────────

    [Test]
    public void AllOptionsClasses_AreSealed()
    {
        Assert.That(typeof(TemporalOptions).IsSealed, Is.True);
        Assert.That(typeof(PipelineOptions).IsSealed, Is.True);
        Assert.That(typeof(JwtOptions).IsSealed, Is.True);
        Assert.That(typeof(DisasterRecoveryOptions).IsSealed, Is.True);
    }
}
