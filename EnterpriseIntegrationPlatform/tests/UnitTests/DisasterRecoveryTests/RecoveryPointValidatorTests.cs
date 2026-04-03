using EnterpriseIntegrationPlatform.DisasterRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.DisasterRecoveryTests;

[TestFixture]
public class RecoveryPointValidatorTests
{
    private RecoveryPointValidator _sut = null!;
    private FakeTimeProvider _timeProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new RecoveryPointValidator(
            NullLogger<RecoveryPointValidator>.Instance,
            _timeProvider);
    }

    [Test]
    public async Task RegisterObjectiveAsync_NewObjective_CanBeRetrieved()
    {
        var objective = new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        };

        await _sut.RegisterObjectiveAsync(objective);

        var objectives = await _sut.GetObjectivesAsync();
        Assert.That(objectives, Has.Count.EqualTo(1));
        Assert.That(objectives[0].ObjectiveId, Is.EqualTo("gold"));
    }

    [Test]
    public void RegisterObjectiveAsync_NullObjective_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RegisterObjectiveAsync(null!));
    }

    [Test]
    public async Task ValidateAsync_RpoAndRtoMet_ReturnsPassed()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });

        var result = await _sut.ValidateAsync("gold", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(2));

        Assert.That(result.Passed, Is.True);
        Assert.That(result.RpoMet, Is.True);
        Assert.That(result.RtoMet, Is.True);
    }

    [Test]
    public async Task ValidateAsync_RpoViolated_ReturnsNotPassed()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });

        var result = await _sut.ValidateAsync("gold", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

        Assert.That(result.Passed, Is.False);
        Assert.That(result.RpoMet, Is.False);
        Assert.That(result.RtoMet, Is.True);
    }

    [Test]
    public async Task ValidateAsync_RtoViolated_ReturnsNotPassed()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });

        var result = await _sut.ValidateAsync("gold", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));

        Assert.That(result.Passed, Is.False);
        Assert.That(result.RpoMet, Is.True);
        Assert.That(result.RtoMet, Is.False);
    }

    [Test]
    public async Task ValidateAsync_BothViolated_ReturnsNotPassed()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });

        var result = await _sut.ValidateAsync("gold", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15));

        Assert.That(result.Passed, Is.False);
        Assert.That(result.RpoMet, Is.False);
        Assert.That(result.RtoMet, Is.False);
    }

    [Test]
    public void ValidateAsync_UnknownObjective_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ValidateAsync("nonexistent", TimeSpan.Zero, TimeSpan.Zero));
    }

    [Test]
    public void ValidateAsync_NullObjectiveId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ValidateAsync(null!, TimeSpan.Zero, TimeSpan.Zero));
    }

    [Test]
    public async Task ValidateAllAsync_MultipleObjectives_ReturnsAllResults()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "gold",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "silver",
            Rpo = TimeSpan.FromMinutes(5),
            Rto = TimeSpan.FromMinutes(15)
        });

        var results = await _sut.ValidateAllAsync(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3));

        Assert.That(results, Has.Count.EqualTo(2));
        var gold = results.First(r => r.Objective.ObjectiveId == "gold");
        var silver = results.First(r => r.Objective.ObjectiveId == "silver");

        Assert.That(gold.Passed, Is.False); // RPO violated: 1min > 30s
        Assert.That(silver.Passed, Is.True); // Both met
    }

    [Test]
    public async Task ValidateAllAsync_NoObjectives_ReturnsEmptyList()
    {
        var results = await _sut.ValidateAllAsync(TimeSpan.Zero, TimeSpan.Zero);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task ValidateAsync_ExactBoundary_ReturnsMet()
    {
        await _sut.RegisterObjectiveAsync(new RecoveryObjective
        {
            ObjectiveId = "boundary",
            Rpo = TimeSpan.FromSeconds(30),
            Rto = TimeSpan.FromMinutes(5)
        });

        var result = await _sut.ValidateAsync("boundary", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

        Assert.That(result.Passed, Is.True);
        Assert.That(result.RpoMet, Is.True);
        Assert.That(result.RtoMet, Is.True);
    }

    [Test]
    public async Task GetObjectivesAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetObjectivesAsync(cts.Token));
    }
}
