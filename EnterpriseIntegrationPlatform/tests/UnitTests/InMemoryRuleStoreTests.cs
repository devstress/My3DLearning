using EnterpriseIntegrationPlatform.RuleEngine;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class InMemoryRuleStoreTests
{
    private InMemoryRuleStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryRuleStore();
    }

    private static BusinessRule CreateRule(string name, int priority = 1) =>
        new()
        {
            Name = name,
            Priority = priority,
            Conditions =
            [
                new RuleCondition
                {
                    FieldName = "MessageType",
                    Operator = RuleConditionOperator.Equals,
                    Value = "Test",
                },
            ],
            Action = new RuleAction { ActionType = RuleActionType.Route, TargetTopic = "test" },
        };

    [Test]
    public async Task AddOrUpdateAsync_NewRule_StoresRule()
    {
        var rule = CreateRule("R1");

        await _store.AddOrUpdateAsync(rule);

        var stored = await _store.GetByNameAsync("R1");
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Name, Is.EqualTo("R1"));
    }

    [Test]
    public async Task AddOrUpdateAsync_ExistingRule_OverwritesRule()
    {
        var rule1 = CreateRule("R1", priority: 1);
        var rule2 = CreateRule("R1", priority: 99);

        await _store.AddOrUpdateAsync(rule1);
        await _store.AddOrUpdateAsync(rule2);

        var stored = await _store.GetByNameAsync("R1");
        Assert.That(stored!.Priority, Is.EqualTo(99));
    }

    [Test]
    public async Task GetAllAsync_ReturnsRulesSortedByPriority()
    {
        await _store.AddOrUpdateAsync(CreateRule("C", 30));
        await _store.AddOrUpdateAsync(CreateRule("A", 10));
        await _store.AddOrUpdateAsync(CreateRule("B", 20));

        var all = await _store.GetAllAsync();

        Assert.That(all, Has.Count.EqualTo(3));
        Assert.That(all[0].Name, Is.EqualTo("A"));
        Assert.That(all[1].Name, Is.EqualTo("B"));
        Assert.That(all[2].Name, Is.EqualTo("C"));
    }

    [Test]
    public async Task GetByNameAsync_NonExistentRule_ReturnsNull()
    {
        var result = await _store.GetByNameAsync("NonExistent");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByNameAsync_IsCaseInsensitive()
    {
        await _store.AddOrUpdateAsync(CreateRule("MyRule"));

        var result = await _store.GetByNameAsync("myrule");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("MyRule"));
    }

    [Test]
    public async Task RemoveAsync_ExistingRule_ReturnsTrue()
    {
        await _store.AddOrUpdateAsync(CreateRule("R1"));

        var removed = await _store.RemoveAsync("R1");

        Assert.That(removed, Is.True);
        Assert.That(await _store.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveAsync_NonExistentRule_ReturnsFalse()
    {
        var removed = await _store.RemoveAsync("NonExistent");

        Assert.That(removed, Is.False);
    }

    [Test]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        await _store.AddOrUpdateAsync(CreateRule("R1"));
        await _store.AddOrUpdateAsync(CreateRule("R2"));
        await _store.AddOrUpdateAsync(CreateRule("R3"));

        var count = await _store.CountAsync();

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task CountAsync_EmptyStore_ReturnsZero()
    {
        var count = await _store.CountAsync();

        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void GetByNameAsync_NullName_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _store.GetByNameAsync(null!));
    }

    [Test]
    public void GetByNameAsync_EmptyName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _store.GetByNameAsync(""));
    }

    [Test]
    public void AddOrUpdateAsync_NullRule_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _store.AddOrUpdateAsync(null!));
    }

    [Test]
    public void RemoveAsync_NullName_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _store.RemoveAsync(null!));
    }
}
