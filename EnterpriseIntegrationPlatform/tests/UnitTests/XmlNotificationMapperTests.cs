using EnterpriseIntegrationPlatform.Activities;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class XmlNotificationMapperTests
{
    private XmlNotificationMapper _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new XmlNotificationMapper();
    }

    // ── Use Case 2: Channel Adapter success → <Ack>ok</Ack> ───────────────

    [Test]
    public void MapAck_ReturnsXmlAckOk()
    {
        var result = _sut.MapAck(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.EqualTo("<Ack>ok</Ack>"));
    }

    // ── Use Case 3: Channel Adapter failure → <Nack>not ok because of {ErrorMessage}</Nack>

    [Test]
    public void MapNack_ReturnsXmlNackWithErrorMessage()
    {
        var result = _sut.MapNack(Guid.NewGuid(), Guid.NewGuid(), "Connection timed out");

        Assert.That(result, Is.EqualTo("<Nack>not ok because of Connection timed out</Nack>"));
    }

    [Test]
    public void MapNack_EscapesXmlSpecialCharactersInErrorMessage()
    {
        var result = _sut.MapNack(
            Guid.NewGuid(), Guid.NewGuid(),
            "Error: <timeout> at endpoint & retry=\"3\"");

        Assert.That(result, Does.Contain("&lt;timeout&gt;"));
        Assert.That(result, Does.Contain("&amp;"));
        Assert.That(result, Does.StartWith("<Nack>not ok because of "));
        Assert.That(result, Does.EndWith("</Nack>"));
    }

    [Test]
    public void MapNack_HandlesEmptyErrorMessage()
    {
        var result = _sut.MapNack(Guid.NewGuid(), Guid.NewGuid(), "");

        Assert.That(result, Is.EqualTo("<Nack>not ok because of </Nack>"));
    }

    [Test]
    public void MapAck_IsIdempotent_SameFormatRegardlessOfInput()
    {
        var result1 = _sut.MapAck(Guid.NewGuid(), Guid.NewGuid());
        var result2 = _sut.MapAck(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result1, Is.EqualTo("<Ack>ok</Ack>"));
    }
}
