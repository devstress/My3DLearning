using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.CompetingConsumersTests;

[TestFixture]
public class InMemoryConsumerScalerTests
{
    private InMemoryConsumerScaler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance);
    }

    [Test]
    public void CurrentCount_DefaultInitialCount_ReturnsOne()
    {
        Assert.That(_sut.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_CustomInitialCount_ReturnsProvidedValue()
    {
        var scaler = new InMemoryConsumerScaler(
            NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 5);

        Assert.That(scaler.CurrentCount, Is.EqualTo(5));
    }

    [Test]
    public async Task ScaleAsync_ScaleUp_UpdatesCurrentCount()
    {
        await _sut.ScaleAsync(5, CancellationToken.None);

        Assert.That(_sut.CurrentCount, Is.EqualTo(5));
    }

    [Test]
    public async Task ScaleAsync_ScaleDown_UpdatesCurrentCount()
    {
        await _sut.ScaleAsync(10, CancellationToken.None);
        await _sut.ScaleAsync(3, CancellationToken.None);

        Assert.That(_sut.CurrentCount, Is.EqualTo(3));
    }

    [Test]
    public async Task ScaleAsync_SameCount_NoChange()
    {
        await _sut.ScaleAsync(1, CancellationToken.None);

        Assert.That(_sut.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void ScaleAsync_ZeroDesired_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ScaleAsync(0, CancellationToken.None));
    }

    [Test]
    public void ScaleAsync_NegativeDesired_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ScaleAsync(-1, CancellationToken.None));
    }

    [Test]
    public void Constructor_ZeroInitialCount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InMemoryConsumerScaler(NullLogger<InMemoryConsumerScaler>.Instance, initialCount: 0));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InMemoryConsumerScaler(null!));
    }

    [Test]
    public async Task ScaleAsync_ConcurrentScaling_ResultsInConsistentState()
    {
        var tasks = Enumerable.Range(1, 20).Select(i =>
            _sut.ScaleAsync((i % 10) + 1, CancellationToken.None));

        await Task.WhenAll(tasks);

        Assert.That(_sut.CurrentCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(_sut.CurrentCount, Is.LessThanOrEqualTo(10));
    }

    [Test]
    public void ScaleAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ScaleAsync(5, cts.Token));
    }
}
