using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class DefaultMessageValidationServiceTests
{
    private DefaultMessageValidationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new DefaultMessageValidationService();
    }

    [Test]
    public async Task ValidateAsync_EmptyMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("", """{"id":1}""");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Message type must not be empty."));
    }

    [Test]
    public async Task ValidateAsync_WhitespaceMessageType_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("   ", """{"id":1}""");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Message type must not be empty."));
    }

    [Test]
    public async Task ValidateAsync_EmptyPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Payload must not be empty."));
    }

    [Test]
    public async Task ValidateAsync_WhitespacePayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "   ");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Payload must not be empty."));
    }

    [Test]
    public async Task ValidateAsync_NonJsonPayload_ReturnsFailure()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "plain text");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Payload is not valid JSON."));
    }

    [Test]
    public async Task ValidateAsync_ValidJsonObject_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """{"id":1}""");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_ValidJsonArray_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", "[1,2,3]");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_WhitespaceBeforeJson_ReturnsSuccess()
    {
        var result = await _sut.ValidateAsync("OrderCreated", """  {"id":1}""");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }
}

[TestFixture]
public sealed class DefaultCompensationActivityServiceTests
{
    private DefaultCompensationActivityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var logger = NullLogger<DefaultCompensationActivityService>.Instance;
        _sut = new DefaultCompensationActivityService(logger);
    }

    [Test]
    public async Task CompensateAsync_ValidInput_ReturnsTrue()
    {
        var result = await _sut.CompensateAsync(Guid.NewGuid(), "DebitAccount");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CompensateAsync_AnyStepName_ReturnsTrue()
    {
        var result = await _sut.CompensateAsync(Guid.NewGuid(), "AnyArbitraryStep");

        Assert.That(result, Is.True);
    }
}

[TestFixture]
public sealed class MessageValidationResultTests
{
    [Test]
    public void Success_IsValid_ReturnsTrue()
    {
        var result = MessageValidationResult.Success;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public void Failure_IsNotValid_ReturnsFalseWithReason()
    {
        var result = MessageValidationResult.Failure("Something went wrong.");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Something went wrong."));
    }
}

[TestFixture]
public sealed class MessageHistoryEntryTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new MessageHistoryEntry("Validate", timestamp, MessageHistoryStatus.Completed, "All good");

        Assert.That(entry.ActivityName, Is.EqualTo("Validate"));
        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Status, Is.EqualTo(MessageHistoryStatus.Completed));
        Assert.That(entry.Detail, Is.EqualTo("All good"));
    }

    [Test]
    public void Status_Enum_HasExpectedValues()
    {
        Assert.That((int)MessageHistoryStatus.Completed, Is.EqualTo(0));
        Assert.That((int)MessageHistoryStatus.Skipped, Is.EqualTo(1));
        Assert.That((int)MessageHistoryStatus.Failed, Is.EqualTo(2));
        Assert.That((int)MessageHistoryStatus.InProgress, Is.EqualTo(3));
    }
}
