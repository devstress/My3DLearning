using NUnit.Framework;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

[TestFixture]
public class DefaultMessageValidationServiceTests
{
    private readonly DefaultMessageValidationService _sut = new();

    [Test]
    public async Task ValidateAsync_WithValidJsonObject_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """{"orderId": 1}""");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_WithValidJsonArray_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("BatchItems", """[{"id": 1}]""");

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task ValidateAsync_WithEmptyMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("", """{"orderId": 1}""");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("Message type"));
    }

    [Test]
    public async Task ValidateAsync_WithWhitespaceMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("   ", """{"orderId": 1}""");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ValidateAsync_WithEmptyPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("Payload"));
    }

    [Test]
    public async Task ValidateAsync_WithNonJsonPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "not json");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Does.Contain("not valid JSON"));
    }

    [Test]
    public async Task ValidateAsync_WithWhitespaceBeforeJson_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """  {"orderId": 1}""");

        Assert.That(result.IsValid, Is.True);
    }
}
