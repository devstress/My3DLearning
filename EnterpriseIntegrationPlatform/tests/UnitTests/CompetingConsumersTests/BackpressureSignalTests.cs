using EnterpriseIntegrationPlatform.Processing.CompetingConsumers;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.CompetingConsumersTests;

[TestFixture]
public class BackpressureSignalTests
{
    private BackpressureSignal _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new BackpressureSignal();
    }

    [Test]
    public void IsBackpressured_Default_ReturnsFalse()
    {
        Assert.That(_sut.IsBackpressured, Is.False);
    }

    [Test]
    public void Signal_WhenCalled_SetsBackpressuredTrue()
    {
        _sut.Signal();

        Assert.That(_sut.IsBackpressured, Is.True);
    }

    [Test]
    public void Release_AfterSignal_SetsBackpressuredFalse()
    {
        _sut.Signal();
        _sut.Release();

        Assert.That(_sut.IsBackpressured, Is.False);
    }

    [Test]
    public void Signal_CalledMultipleTimes_RemainsTrue()
    {
        _sut.Signal();
        _sut.Signal();
        _sut.Signal();

        Assert.That(_sut.IsBackpressured, Is.True);
    }

    [Test]
    public void Release_WithoutPriorSignal_RemainsFalse()
    {
        _sut.Release();

        Assert.That(_sut.IsBackpressured, Is.False);
    }

    [Test]
    public void Signal_ConcurrentSignalAndRelease_DoesNotThrow()
    {
        var barrier = new Barrier(2);

        var signalTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < 10_000; i++)
            {
                _sut.Signal();
            }
        });

        var releaseTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < 10_000; i++)
            {
                _sut.Release();
            }
        });

        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(signalTask, releaseTask));
    }

    [Test]
    public void IsBackpressured_ConcurrentReads_DoesNotThrow()
    {
        _sut.Signal();

        var tasks = Enumerable.Range(0, 100).Select(_ =>
            Task.Run(() =>
            {
                var unused = _sut.IsBackpressured;
            }));

        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }
}
