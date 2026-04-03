using EnterpriseIntegrationPlatform.RuleEngine;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RuleEngineOptionsTests
{
    [Test]
    public void Defaults_EnabledIsTrue()
    {
        var options = new RuleEngineOptions();

        Assert.That(options.Enabled, Is.True);
    }

    [Test]
    public void Defaults_MaxRulesPerEvaluationIsZero()
    {
        var options = new RuleEngineOptions();

        Assert.That(options.MaxRulesPerEvaluation, Is.EqualTo(0));
    }

    [Test]
    public void Defaults_RulesIsEmpty()
    {
        var options = new RuleEngineOptions();

        Assert.That(options.Rules, Is.Empty);
    }

    [Test]
    public void Defaults_RegexTimeoutIsFiveSeconds()
    {
        var options = new RuleEngineOptions();

        Assert.That(options.RegexTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void SectionName_IsRuleEngine()
    {
        Assert.That(RuleEngineOptions.SectionName, Is.EqualTo("RuleEngine"));
    }
}
