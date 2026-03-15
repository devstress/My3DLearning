using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Workflow;

public class DefaultMessageValidationServiceTests
{
    private readonly DefaultMessageValidationService _sut = new();

    [Fact]
    public async Task ValidateAsync_WithValidJsonObject_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """{"orderId": 1}""");

        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidJsonArray_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("BatchItems", """[{"id": 1}]""");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("", """{"orderId": 1}""");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("Message type");
    }

    [Fact]
    public async Task ValidateAsync_WithWhitespaceMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("   ", """{"orderId": 1}""");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("Payload");
    }

    [Fact]
    public async Task ValidateAsync_WithNonJsonPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "not json");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not valid JSON");
    }

    [Fact]
    public async Task ValidateAsync_WithWhitespaceBeforeJson_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """  {"orderId": 1}""");

        result.IsValid.Should().BeTrue();
    }
}
