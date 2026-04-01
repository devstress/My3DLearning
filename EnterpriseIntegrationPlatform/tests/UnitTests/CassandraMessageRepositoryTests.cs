using Cassandra;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Storage.Cassandra;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class CassandraMessageRepositoryTests
{
    private readonly ICassandraSessionFactory _sessionFactory = Substitute.For<ICassandraSessionFactory>();
    private readonly ISession _session = Substitute.For<ISession>();
    private readonly ILogger<CassandraMessageRepository> _logger = Substitute.For<ILogger<CassandraMessageRepository>>();
    private readonly CassandraMessageRepository _repository;

    public CassandraMessageRepositoryTests()
    {
        _sessionFactory.GetSessionAsync(Arg.Any<CancellationToken>()).Returns(_session);
        _repository = new CassandraMessageRepository(_sessionFactory, _logger);
    }

    [Test]
    public async Task SaveMessageAsync_ExecutesBatch()
    {
        // Arrange
        var record = CreateRecord();
        var rowSet = Substitute.For<RowSet>();
        _session.ExecuteAsync(Arg.Any<BatchStatement>()).Returns(rowSet);

        // Act
        await _repository.SaveMessageAsync(record);

        // Assert
        await _session.Received(1).ExecuteAsync(Arg.Any<BatchStatement>());
    }

    [Test]
    public async Task SaveFaultAsync_ExecutesStatement()
    {
        // Arrange
        var fault = CreateFault();
        var rowSet = Substitute.For<RowSet>();
        _session.ExecuteAsync(Arg.Any<SimpleStatement>()).Returns(rowSet);

        // Act
        await _repository.SaveFaultAsync(fault);

        // Assert
        await _session.Received(1).ExecuteAsync(Arg.Any<SimpleStatement>());
    }

    [Test]
    public async Task GetByCorrelationIdAsync_ReturnsEmptyList_WhenNoResults()
    {
        // Arrange
        var rowSet = Substitute.For<RowSet>();
        rowSet.GetEnumerator().Returns(new List<Row>().GetEnumerator());
        _session.ExecuteAsync(Arg.Any<SimpleStatement>()).Returns(rowSet);

        // Act
        var result = await _repository.GetByCorrelationIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetByMessageIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var rowSet = Substitute.For<RowSet>();
        rowSet.GetEnumerator().Returns(new List<Row>().GetEnumerator());
        _session.ExecuteAsync(Arg.Any<SimpleStatement>()).Returns(rowSet);

        // Act
        var result = await _repository.GetByMessageIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetFaultsByCorrelationIdAsync_ReturnsEmptyList_WhenNoResults()
    {
        // Arrange
        var rowSet = Substitute.For<RowSet>();
        rowSet.GetEnumerator().Returns(new List<Row>().GetEnumerator());
        _session.ExecuteAsync(Arg.Any<SimpleStatement>()).Returns(rowSet);

        // Act
        var result = await _repository.GetFaultsByCorrelationIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task UpdateDeliveryStatusAsync_ExecutesBatch()
    {
        // Arrange
        var rowSet = Substitute.For<RowSet>();
        _session.ExecuteAsync(Arg.Any<BatchStatement>()).Returns(rowSet);

        // Act
        await _repository.UpdateDeliveryStatusAsync(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DeliveryStatus.Delivered);

        // Assert
        await _session.Received(1).ExecuteAsync(Arg.Any<BatchStatement>());
    }

    [Test]
    public async Task SaveMessageAsync_ObtainsSessionFromFactory()
    {
        // Arrange
        var record = CreateRecord();
        var rowSet = Substitute.For<RowSet>();
        _session.ExecuteAsync(Arg.Any<BatchStatement>()).Returns(rowSet);

        // Act
        await _repository.SaveMessageAsync(record);

        // Assert
        await _sessionFactory.Received(1).GetSessionAsync(Arg.Any<CancellationToken>());
    }

    private static MessageRecord CreateRecord(
        Guid? messageId = null,
        Guid? correlationId = null,
        DeliveryStatus status = DeliveryStatus.Pending)
    {
        return new MessageRecord
        {
            MessageId = messageId ?? Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            RecordedAt = DateTimeOffset.UtcNow,
            Source = "Gateway",
            MessageType = "OrderShipment",
            PayloadJson = """{"orderId":"TEST-001"}""",
            DeliveryStatus = status,
        };
    }

    private static FaultEnvelope CreateFault(Guid? correlationId = null)
    {
        return new FaultEnvelope
        {
            FaultId = Guid.NewGuid(),
            OriginalMessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            OriginalMessageType = "OrderShipment",
            FaultedBy = "Processing.Routing",
            FaultReason = "Schema validation failed",
            FaultedAt = DateTimeOffset.UtcNow,
            RetryCount = 3,
            ErrorDetails = "System.FormatException: Invalid format",
        };
    }
}
