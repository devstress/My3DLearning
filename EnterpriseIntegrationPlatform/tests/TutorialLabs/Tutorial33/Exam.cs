// ============================================================================
// Tutorial 33 – Security (Exam)
// ============================================================================
// Coding challenges: SQL injection sanitization, secret rotation with
// SecretRotationService, and PayloadSizeOptions defaults with custom limits.
// ============================================================================

using EnterpriseIntegrationPlatform.Security;
using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial33;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: SQL Injection Sanitization ─────────────────────────────

    [Test]
    public void Challenge1_SqlInjection_Sanitized()
    {
        var sanitizer = new InputSanitizer();

        // SQL injection patterns should be detected as unclean
        Assert.That(sanitizer.IsClean("'; DROP TABLE users"), Is.False);
        Assert.That(sanitizer.IsClean("1 OR 1=1"), Is.False);
        Assert.That(sanitizer.IsClean("UNION SELECT * FROM passwords"), Is.False);

        // Sanitize removes the SQL injection pattern
        var sanitized = sanitizer.Sanitize("Hello '; DROP TABLE users --");
        Assert.That(sanitized, Does.Not.Contain("DROP TABLE"));
    }

    // ── Challenge 2: Secret Rotation ────────────────────────────────────────

    [Test]
    public async Task Challenge2_SecretRotation_WithRotationService()
    {
        var auditLogger = new SecretAuditLogger(NullLogger<SecretAuditLogger>.Instance);
        var provider = new InMemorySecretProvider(auditLogger);
        var secretsOptions = Options.Create(new SecretsOptions());
        var rotationService = new SecretRotationService(
            provider, auditLogger, secretsOptions,
            NullLogger<SecretRotationService>.Instance);

        // Store an initial secret
        var initial = await provider.SetSecretAsync("api-key", "original-value");
        Assert.That(initial.Value, Is.EqualTo("original-value"));

        // Rotate now
        var rotated = await rotationService.RotateNowAsync("api-key");

        // Verify the secret was rotated to a new value
        Assert.That(rotated.Key, Is.EqualTo("api-key"));
        Assert.That(rotated.Value, Is.Not.EqualTo("original-value"));
        Assert.That(rotated.Version, Is.Not.EqualTo(initial.Version));

        // Verify the rotated value is persisted
        var current = await provider.GetSecretAsync("api-key");
        Assert.That(current!.Value, Is.EqualTo(rotated.Value));
    }

    // ── Challenge 3: PayloadSizeOptions Defaults and Custom Enforcement ─────

    [Test]
    public void Challenge3_PayloadSizeOptions_DefaultsAndCustom()
    {
        // Verify defaults
        var defaults = new PayloadSizeOptions();
        Assert.That(defaults.MaxPayloadBytes, Is.EqualTo(1_048_576)); // 1 MB

        // Custom limit of 10 bytes
        var guard = new PayloadSizeGuard(
            Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 10 }));

        // Small payload passes
        Assert.DoesNotThrow(() => guard.Enforce("tiny"));

        // Oversized payload throws with correct sizes
        var ex = Assert.Throws<PayloadTooLargeException>(
            () => guard.Enforce("this is way too long for a 10-byte limit"));

        Assert.That(ex!.MaxBytes, Is.EqualTo(10));
        Assert.That(ex.ActualBytes, Is.GreaterThan(10));
    }
}
