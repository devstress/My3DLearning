using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.CompetingConsumersTests;

[TestFixture]
public class InMemoryConsumerLagMonitorTests
{
    private InMemoryConsumerLagMonitor _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new InMemoryConsumerLagMonitor();
    }

    [Test]
    public async Task GetLagAsync_NoDataReported_ReturnsZeroLag()
    {
        var result = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);

        Assert.That(result.CurrentLag, Is.EqualTo(0));
        Assert.That(result.Topic, Is.EqualTo("orders"));
        Assert.That(result.ConsumerGroup, Is.EqualTo("group-1"));
    }

    [Test]
    public async Task ReportLagAsync_SingleReport_GetLagReturnsReportedValue()
    {
        var lag = new ConsumerLagInfo("group-1", "orders", 500, DateTimeOffset.UtcNow);
        await _sut.ReportLagAsync(lag);

        var result = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);

        Assert.That(result.CurrentLag, Is.EqualTo(500));
    }

    [Test]
    public async Task ReportLagAsync_MultipleReports_GetLagReturnsLatestValue()
    {
        var lag1 = new ConsumerLagInfo("group-1", "orders", 500, DateTimeOffset.UtcNow);
        var lag2 = new ConsumerLagInfo("group-1", "orders", 1500, DateTimeOffset.UtcNow);

        await _sut.ReportLagAsync(lag1);
        await _sut.ReportLagAsync(lag2);

        var result = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);

        Assert.That(result.CurrentLag, Is.EqualTo(1500));
    }

    [Test]
    public async Task GetLagAsync_DifferentTopics_ReturnsIndependentValues()
    {
        var lagOrders = new ConsumerLagInfo("group-1", "orders", 100, DateTimeOffset.UtcNow);
        var lagEvents = new ConsumerLagInfo("group-1", "events", 900, DateTimeOffset.UtcNow);

        await _sut.ReportLagAsync(lagOrders);
        await _sut.ReportLagAsync(lagEvents);

        var ordersResult = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);
        var eventsResult = await _sut.GetLagAsync("events", "group-1", CancellationToken.None);

        Assert.That(ordersResult.CurrentLag, Is.EqualTo(100));
        Assert.That(eventsResult.CurrentLag, Is.EqualTo(900));
    }

    [Test]
    public async Task GetLagAsync_DifferentConsumerGroups_ReturnsIndependentValues()
    {
        var lagGroup1 = new ConsumerLagInfo("group-1", "orders", 200, DateTimeOffset.UtcNow);
        var lagGroup2 = new ConsumerLagInfo("group-2", "orders", 800, DateTimeOffset.UtcNow);

        await _sut.ReportLagAsync(lagGroup1);
        await _sut.ReportLagAsync(lagGroup2);

        var group1Result = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);
        var group2Result = await _sut.GetLagAsync("orders", "group-2", CancellationToken.None);

        Assert.That(group1Result.CurrentLag, Is.EqualTo(200));
        Assert.That(group2Result.CurrentLag, Is.EqualTo(800));
    }

    [Test]
    public async Task ReportLagAsync_ConcurrentWrites_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _sut.ReportLagAsync(
                new ConsumerLagInfo("group-1", "orders", i, DateTimeOffset.UtcNow)));

        await Task.WhenAll(tasks);

        var result = await _sut.GetLagAsync("orders", "group-1", CancellationToken.None);
        Assert.That(result.CurrentLag, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.CurrentLag, Is.LessThan(100));
    }

    [Test]
    public void ReportLagAsync_NullLagInfo_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ReportLagAsync(null!));
    }

    [Test]
    public void GetLagAsync_NullTopic_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetLagAsync(null!, "group-1", CancellationToken.None));
    }

    [Test]
    public void GetLagAsync_EmptyConsumerGroup_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetLagAsync("orders", "", CancellationToken.None));
    }

    [Test]
    public void GetLagAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetLagAsync("orders", "group-1", cts.Token));
    }
}
