using NSubstitute;
using NUnit.Framework;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public sealed class IntegrationActivitiesTests
{
    private IMessageValidationService _validation = null!;
    private IMessageLoggingService _logging = null!;
    private IntegrationActivities _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _validation = Substitute.For<IMessageValidationService>();
        _logging = Substitute.For<IMessageLoggingService>();
        _sut = new IntegrationActivities(_validation, _logging);
    }

    [Test]
    public async Task ValidateMessageAsync_DelegatesToValidationService()
    {
        var expected = new MessageValidationResult(true);
        _validation.ValidateAsync("OrderMessage", "{}")
            .Returns(expected);

        await _sut.ValidateMessageAsync("OrderMessage", "{}");

        await _validation.Received(1).ValidateAsync("OrderMessage", "{}");
    }

    [Test]
    public async Task ValidateMessageAsync_ReturnsServiceResult()
    {
        var expected = MessageValidationResult.Failure("Field 'Id' is required");
        _validation.ValidateAsync("InvoiceMessage", "{\"amount\":0}")
            .Returns(expected);

        var result = await _sut.ValidateMessageAsync("InvoiceMessage", "{\"amount\":0}");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Reason, Is.EqualTo("Field 'Id' is required"));
    }

    [Test]
    public async Task LogProcessingStageAsync_DelegatesToLoggingService()
    {
        var messageId = Guid.NewGuid();

        await _sut.LogProcessingStageAsync(messageId, "OrderMessage", "Validated");

        await _logging.Received(1).LogAsync(messageId, "OrderMessage", "Validated");
    }
}
