// ============================================================================
// Tutorial 43 – Kubernetes Deployment / Configuration Options (Exam)
// ============================================================================
// Coding challenges: full deployment config, JWT security configuration,
// and Temporal + Pipeline combined configuration scenario.
// ============================================================================

using EnterpriseIntegrationPlatform.Demo.Pipeline;
using EnterpriseIntegrationPlatform.DisasterRecovery;
using EnterpriseIntegrationPlatform.Security;
using EnterpriseIntegrationPlatform.Workflow.Temporal;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial43;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full Deployment Config Round-Trip ───────────────────────

    [Test]
    public void Challenge1_FullDeploymentConfig_SetAllOptionsVerifyRoundTrip()
    {
        var temporal = Options.Create(new TemporalOptions
        {
            ServerAddress = "temporal.k8s.internal:7233",
            Namespace = "production",
            TaskQueue = "prod-workflows",
        });

        var pipeline = Options.Create(new PipelineOptions
        {
            NatsUrl = "nats://nats.k8s.internal:4222",
            InboundSubject = "prod.inbound",
            AckSubject = "prod.ack",
            NackSubject = "prod.nack",
            ConsumerGroup = "prod-consumers",
            TemporalServerAddress = "temporal.k8s.internal:7233",
            TemporalNamespace = "production",
            TemporalTaskQueue = "prod-workflows",
            WorkflowTimeout = TimeSpan.FromMinutes(10),
        });

        var jwt = Options.Create(new JwtOptions
        {
            Issuer = "https://auth.example.com",
            Audience = "eip-api",
            SigningKey = "dGVzdC1rZXktZm9yLWp3dC1zaWduaW5n",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        });

        var dr = Options.Create(new DisasterRecoveryOptions
        {
            MaxDrillHistorySize = 50,
            MaxReplicationLag = TimeSpan.FromSeconds(15),
            HealthCheckInterval = TimeSpan.FromSeconds(5),
        });

        Assert.That(temporal.Value.Namespace, Is.EqualTo("production"));
        Assert.That(pipeline.Value.NatsUrl, Is.EqualTo("nats://nats.k8s.internal:4222"));
        Assert.That(pipeline.Value.WorkflowTimeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(jwt.Value.Issuer, Is.EqualTo("https://auth.example.com"));
        Assert.That(dr.Value.MaxDrillHistorySize, Is.EqualTo(50));
    }

    // ── Challenge 2: JWT Security Configuration Validation ──────────────────

    [Test]
    public void Challenge2_JwtSecurityConfiguration_Validation()
    {
        var opts = new JwtOptions
        {
            Issuer = "https://identity.example.com",
            Audience = "api.example.com",
            SigningKey = "c3VwZXItc2VjcmV0LWtleS1mb3ItdGVzdA==",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(3),
        };

        Assert.That(opts.Issuer, Is.Not.Empty);
        Assert.That(opts.Audience, Is.Not.Empty);
        Assert.That(opts.SigningKey, Is.Not.Empty);
        Assert.That(opts.ValidateLifetime, Is.True);
        Assert.That(opts.ClockSkew, Is.LessThanOrEqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(opts.ClockSkew, Is.GreaterThan(TimeSpan.Zero));

        // Verify the section name is correct for config binding
        Assert.That(JwtOptions.SectionName, Is.EqualTo("Jwt"));

        // Verify IOptions wrapping preserves all values
        var wrapped = Options.Create(opts);
        Assert.That(wrapped.Value.Issuer, Is.EqualTo(opts.Issuer));
        Assert.That(wrapped.Value.ClockSkew, Is.EqualTo(opts.ClockSkew));
    }

    // ── Challenge 3: Temporal + Pipeline Combined Configuration ─────────────

    [Test]
    public void Challenge3_TemporalPipeline_CombinedConfiguration()
    {
        var temporal = new TemporalOptions
        {
            ServerAddress = "temporal.cluster:7233",
            Namespace = "integration",
            TaskQueue = "main-queue",
        };

        var pipeline = new PipelineOptions
        {
            TemporalServerAddress = temporal.ServerAddress,
            TemporalNamespace = temporal.Namespace,
            TemporalTaskQueue = temporal.TaskQueue,
            NatsUrl = "nats://nats.cluster:4222",
            InboundSubject = "integration.inbound",
            AckSubject = "integration.ack",
            NackSubject = "integration.nack",
        };

        // Verify the pipeline references match the Temporal config
        Assert.That(pipeline.TemporalServerAddress, Is.EqualTo(temporal.ServerAddress));
        Assert.That(pipeline.TemporalNamespace, Is.EqualTo(temporal.Namespace));
        Assert.That(pipeline.TemporalTaskQueue, Is.EqualTo(temporal.TaskQueue));

        // Verify defaults are overridden
        Assert.That(pipeline.NatsUrl, Is.Not.EqualTo(new PipelineOptions().NatsUrl));

        // Verify NATS subjects are properly configured
        Assert.That(pipeline.AckSubject, Does.StartWith("integration."));
        Assert.That(pipeline.NackSubject, Does.StartWith("integration."));
        Assert.That(pipeline.InboundSubject, Does.StartWith("integration."));
    }
}
