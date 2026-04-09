using EnterpriseIntegrationPlatform.Security;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class JwtOptionsTests
{
    [Test]
    public void SectionName_IsJwt()
    {
        Assert.That(JwtOptions.SectionName, Is.EqualTo("Jwt"));
    }

    [Test]
    public void Default_Issuer_IsEmpty()
    {
        Assert.That(new JwtOptions().Issuer, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Default_Audience_IsEmpty()
    {
        Assert.That(new JwtOptions().Audience, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Default_SigningKey_IsEmpty()
    {
        Assert.That(new JwtOptions().SigningKey, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Default_ValidateLifetime_IsTrue()
    {
        Assert.That(new JwtOptions().ValidateLifetime, Is.True);
    }

    [Test]
    public void Default_ClockSkew_Is5Minutes()
    {
        Assert.That(new JwtOptions().ClockSkew, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void AllProperties_AreSettable()
    {
        var options = new JwtOptions
        {
            Issuer = "my-issuer",
            Audience = "my-audience",
            SigningKey = "secret-key-base64",
            ValidateLifetime = false,
            ClockSkew = TimeSpan.FromMinutes(10),
        };

        Assert.That(options.Issuer, Is.EqualTo("my-issuer"));
        Assert.That(options.Audience, Is.EqualTo("my-audience"));
        Assert.That(options.SigningKey, Is.EqualTo("secret-key-base64"));
        Assert.That(options.ValidateLifetime, Is.False);
        Assert.That(options.ClockSkew, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }
}
