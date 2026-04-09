using EnterpriseIntegrationPlatform.Gateway.Api;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.Gateway;

[TestFixture]
public class GatewayResponseTests
{
    [Test]
    public void GatewayResponse_SuccessCase()
    {
        var id = Guid.NewGuid();
        var response = new GatewayResponse
        {
            CorrelationId = id,
            Success = true,
            StatusCode = 200,
        };

        Assert.That(response.CorrelationId, Is.EqualTo(id));
        Assert.That(response.Success, Is.True);
        Assert.That(response.StatusCode, Is.EqualTo(200));
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void GatewayResponse_FailureCase()
    {
        var response = new GatewayResponse
        {
            CorrelationId = Guid.NewGuid(),
            Success = false,
            StatusCode = 502,
            Error = "Downstream unavailable",
        };

        Assert.That(response.Success, Is.False);
        Assert.That(response.StatusCode, Is.EqualTo(502));
        Assert.That(response.Error, Is.EqualTo("Downstream unavailable"));
    }

    [Test]
    public void GatewayResponseT_SuccessWithPayload()
    {
        var id = Guid.NewGuid();
        var response = new GatewayResponse<string>
        {
            CorrelationId = id,
            Success = true,
            StatusCode = 200,
            Payload = "hello",
        };

        Assert.That(response.CorrelationId, Is.EqualTo(id));
        Assert.That(response.Success, Is.True);
        Assert.That(response.Payload, Is.EqualTo("hello"));
    }

    [Test]
    public void GatewayResponseT_FailureNoPayload()
    {
        var response = new GatewayResponse<int>
        {
            CorrelationId = Guid.NewGuid(),
            Success = false,
            StatusCode = 500,
            Error = "Internal error",
        };

        Assert.That(response.Success, Is.False);
        Assert.That(response.Payload, Is.EqualTo(0));
        Assert.That(response.Error, Is.EqualTo("Internal error"));
    }

    [Test]
    public void GatewayResponseT_NullablePayload_DefaultsToNull()
    {
        var response = new GatewayResponse<object?>
        {
            CorrelationId = Guid.NewGuid(),
            Success = false,
        };

        Assert.That(response.Payload, Is.Null);
    }
}
