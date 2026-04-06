// ============================================================================
// Tutorial 33 – Security (Lab)
// ============================================================================
// This lab exercises InputSanitizer, PayloadSizeGuard, InMemorySecretProvider,
// and SecretEntry to learn the security subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.Security;
using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace TutorialLabs.Tutorial33;

[TestFixture]
public sealed class Lab
{
    private InputSanitizer _sanitizer = null!;

    [SetUp]
    public void SetUp()
    {
        _sanitizer = new InputSanitizer();
    }

    // ── Sanitize Removes XSS Script Tags ────────────────────────────────────

    [Test]
    public void InputSanitizer_Sanitize_RemovesScriptTags()
    {
        var input = "Hello <script>alert('xss')</script> World";

        var result = _sanitizer.Sanitize(input);

        Assert.That(result, Does.Not.Contain("<script>"));
        Assert.That(result, Does.Not.Contain("alert"));
        Assert.That(result, Does.Contain("Hello"));
        Assert.That(result, Does.Contain("World"));
    }

    // ── IsClean Returns False For XSS ───────────────────────────────────────

    [Test]
    public void InputSanitizer_IsClean_ReturnsFalseForXss()
    {
        var dirty = "<script>alert('xss')</script>";

        Assert.That(_sanitizer.IsClean(dirty), Is.False);
    }

    // ── IsClean Returns True For Clean Input ────────────────────────────────

    [Test]
    public void InputSanitizer_IsClean_ReturnsTrueForClean()
    {
        var clean = "Hello, this is perfectly safe text.";

        Assert.That(_sanitizer.IsClean(clean), Is.True);
    }

    // ── PayloadSizeGuard Passes For Small Payload ───────────────────────────

    [Test]
    public void PayloadSizeGuard_Enforce_PassesForSmallPayload()
    {
        var guard = new PayloadSizeGuard(
            Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 1024 }));

        var smallPayload = new string('x', 100);

        Assert.DoesNotThrow(() => guard.Enforce(smallPayload));
    }

    // ── PayloadSizeGuard Throws For Oversized Payload ───────────────────────

    [Test]
    public void PayloadSizeGuard_Enforce_ThrowsPayloadTooLargeException()
    {
        var guard = new PayloadSizeGuard(
            Options.Create(new PayloadSizeOptions { MaxPayloadBytes = 50 }));

        var oversized = new string('x', 200);

        var ex = Assert.Throws<PayloadTooLargeException>(
            () => guard.Enforce(oversized));

        Assert.That(ex!.MaxBytes, Is.EqualTo(50));
        Assert.That(ex.ActualBytes, Is.GreaterThan(50));
    }

    // ── InMemorySecretProvider Set/Get Roundtrip ────────────────────────────

    [Test]
    public async Task SecretProvider_SetAndGet_Roundtrip()
    {
        var provider = new InMemorySecretProvider();

        var stored = await provider.SetSecretAsync("db-password", "s3cret!");
        var retrieved = await provider.GetSecretAsync("db-password");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Key, Is.EqualTo("db-password"));
        Assert.That(retrieved.Value, Is.EqualTo("s3cret!"));
        Assert.That(retrieved.Version, Is.EqualTo(stored.Version));
    }

    // ── SecretEntry Record Has Expected Properties ──────────────────────────

    [Test]
    public void SecretEntry_RecordProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var meta = new Dictionary<string, string> { ["env"] = "prod" };
        var entry = new SecretEntry("api-key", "value123", "3", now, Metadata: meta);

        Assert.That(entry.Key, Is.EqualTo("api-key"));
        Assert.That(entry.Value, Is.EqualTo("value123"));
        Assert.That(entry.Version, Is.EqualTo("3"));
        Assert.That(entry.CreatedAt, Is.EqualTo(now));
        Assert.That(entry.ExpiresAt, Is.Null);
        Assert.That(entry.Metadata!["env"], Is.EqualTo("prod"));
    }
}
