using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class BusinessRuleEngineTests
{
    private InMemoryRuleStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryRuleStore();
    }

    private BusinessRuleEngine BuildEngine(RuleEngineOptions? options = null) =>
        new(
            _store,
            Options.Create(options ?? new RuleEngineOptions()),
            NullLogger<BusinessRuleEngine>.Instance);

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string messageType = "OrderCreated",
        string source = "OrderService",
        MessagePriority priority = MessagePriority.Normal,
        string payloadJson = """{"orderId":1}""",
        Dictionary<string, string>? metadata = null)
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: source,
            messageType: messageType);

        return metadata is not null ? envelope with { Metadata = metadata, Priority = priority } : envelope with { Priority = priority };
    }

    private static BusinessRule CreateRule(
        string name,
        int priority,
        RuleActionType actionType,
        IReadOnlyList<RuleCondition> conditions,
        RuleLogicOperator logicOperator = RuleLogicOperator.And,
        string? targetTopic = null,
        string? transformName = null,
        string? reason = null,
        bool stopOnMatch = true,
        bool enabled = true) =>
        new()
        {
            Name = name,
            Priority = priority,
            LogicOperator = logicOperator,
            Conditions = conditions,
            Action = new RuleAction
            {
                ActionType = actionType,
                TargetTopic = targetTopic,
                TransformName = transformName,
                Reason = reason,
            },
            StopOnMatch = stopOnMatch,
            Enabled = enabled,
        };

    // ------------------------------------------------------------------ //
    // Equals operator
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_EqualsOnMessageType_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders.created");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Route));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("orders.created"));
    }

    [Test]
    public async Task EvaluateAsync_EqualsOnMessageType_IsCaseInsensitive()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "ordercreated" }],
            targetTopic: "orders.created");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_EqualsOnSource_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PaymentService" }],
            targetTopic: "payments");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(source: "PaymentService"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("payments"));
    }

    [Test]
    public async Task EvaluateAsync_EqualsOnPriority_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Priority", Operator = RuleConditionOperator.Equals, Value = "Critical" }],
            targetTopic: "critical");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(priority: MessagePriority.Critical));

        Assert.That(result.HasMatch, Is.True);
    }

    // ------------------------------------------------------------------ //
    // Contains operator
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_ContainsOnMessageType_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "Order" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_ContainsNoMatch_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "Invoice" }],
            targetTopic: "invoices");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Regex operator
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_RegexOnMessageType_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Regex, Value = @"^Order(Created|Updated)$" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderUpdated"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_RegexNoMatch_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Regex, Value = @"^Invoice" }],
            targetTopic: "invoices");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // In operator
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_InOperator_MatchesWhenValueInList()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.In, Value = "OrderCreated,OrderUpdated,OrderDeleted" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderUpdated"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_InOperator_DoesNotMatchWhenValueNotInList()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.In, Value = "InvoiceCreated,InvoiceUpdated" }],
            targetTopic: "invoices");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.False);
    }

    [Test]
    public async Task EvaluateAsync_InOperator_IsCaseInsensitive()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.In, Value = "serviceA, SERVICEB, ServiceC" }],
            targetTopic: "multi");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(source: "serviceb"));

        Assert.That(result.HasMatch, Is.True);
    }

    // ------------------------------------------------------------------ //
    // GreaterThan operator
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_GreaterThanOnPayloadField_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.amount", Operator = RuleConditionOperator.GreaterThan, Value = "100" }],
            targetTopic: "high-value");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"amount":250.50}"""));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_GreaterThanOnPayloadField_DoesNotMatchWhenEqual()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.amount", Operator = RuleConditionOperator.GreaterThan, Value = "100" }],
            targetTopic: "high-value");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"amount":100}"""));

        Assert.That(result.HasMatch, Is.False);
    }

    [Test]
    public async Task EvaluateAsync_GreaterThanOnPayloadField_DoesNotMatchWhenLess()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.amount", Operator = RuleConditionOperator.GreaterThan, Value = "100" }],
            targetTopic: "high-value");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"amount":50}"""));

        Assert.That(result.HasMatch, Is.False);
    }

    [Test]
    public async Task EvaluateAsync_GreaterThanOnNonNumericField_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.status", Operator = RuleConditionOperator.GreaterThan, Value = "100" }],
            targetTopic: "high-value");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"status":"pending"}"""));

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Metadata field conditions
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_MetadataFieldEquals_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Metadata.tenant", Operator = RuleConditionOperator.Equals, Value = "acme" }],
            targetTopic: "acme-topic");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(
            BuildEnvelope(metadata: new Dictionary<string, string> { ["tenant"] = "acme" }));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_MetadataFieldMissing_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Metadata.tenant", Operator = RuleConditionOperator.Equals, Value = "acme" }],
            targetTopic: "acme-topic");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope());

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Payload JSON field conditions
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_PayloadFieldEquals_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.status", Operator = RuleConditionOperator.Equals, Value = "urgent" }],
            targetTopic: "urgent");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"status":"urgent"}"""));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_PayloadNestedFieldEquals_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "Payload.order.priority", Operator = RuleConditionOperator.Equals, Value = "high" }],
            targetTopic: "high-priority");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(payloadJson: """{"order":{"priority":"high","id":42}}"""));

        Assert.That(result.HasMatch, Is.True);
    }

    // ------------------------------------------------------------------ //
    // AND logic
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_AndLogic_AllConditionsMatch_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "OrderService" },
            ],
            logicOperator: RuleLogicOperator.And,
            targetTopic: "orders.created");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated", source: "OrderService"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_AndLogic_OneConditionFails_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PaymentService" },
            ],
            logicOperator: RuleLogicOperator.And,
            targetTopic: "mixed");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated", source: "OrderService"));

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // OR logic
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_OrLogic_OneConditionMatches_Matches()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "InvoiceCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "OrderService" },
            ],
            logicOperator: RuleLogicOperator.Or,
            targetTopic: "combined");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated", source: "OrderService"));

        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_OrLogic_NoConditionMatches_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [
                new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "InvoiceCreated" },
                new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "PaymentService" },
            ],
            logicOperator: RuleLogicOperator.Or,
            targetTopic: "combined");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated", source: "OrderService"));

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Priority ordering
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_MultipleRules_EvaluatesInPriorityOrder()
    {
        var lowPriority = CreateRule("LowPriority", 10, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "Order" }],
            targetTopic: "orders.general");
        var highPriority = CreateRule("HighPriority", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders.created");
        await _store.AddOrUpdateAsync(lowPriority);
        await _store.AddOrUpdateAsync(highPriority);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.MatchedRules, Has.Count.EqualTo(1));
        Assert.That(result.MatchedRules[0].Name, Is.EqualTo("HighPriority"));
        Assert.That(result.Actions[0].TargetTopic, Is.EqualTo("orders.created"));
    }

    // ------------------------------------------------------------------ //
    // StopOnMatch = false (collect all)
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_StopOnMatchFalse_CollectsAllMatches()
    {
        var rule1 = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Contains, Value = "Order" }],
            targetTopic: "orders", stopOnMatch: false);
        var rule2 = CreateRule("R2", 2, RuleActionType.Transform,
            [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "OrderService" }],
            transformName: "enrich-order", stopOnMatch: false);
        await _store.AddOrUpdateAsync(rule1);
        await _store.AddOrUpdateAsync(rule2);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated", source: "OrderService"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.MatchedRules, Has.Count.EqualTo(2));
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Route));
        Assert.That(result.Actions[1].ActionType, Is.EqualTo(RuleActionType.Transform));
    }

    // ------------------------------------------------------------------ //
    // Disabled rules
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_DisabledRule_IsSkipped()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders", enabled: false);
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.RulesEvaluated, Is.EqualTo(0));
    }

    // ------------------------------------------------------------------ //
    // Engine disabled
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_EngineDisabled_ReturnsEmptyResult()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine(new RuleEngineOptions { Enabled = false });

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.RulesEvaluated, Is.EqualTo(0));
    }

    // ------------------------------------------------------------------ //
    // MaxRulesPerEvaluation
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_MaxRulesPerEvaluation_StopsAfterLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            var rule = CreateRule($"R{i}", i, RuleActionType.Route,
                [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "Nomatch" }],
                targetTopic: "none");
            await _store.AddOrUpdateAsync(rule);
        }

        var sut = BuildEngine(new RuleEngineOptions { MaxRulesPerEvaluation = 3 });
        var result = await sut.EvaluateAsync(BuildEnvelope());

        Assert.That(result.RulesEvaluated, Is.EqualTo(3));
        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Action types
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_RejectAction_ReturnsRejectAction()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Reject,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "Spam" }],
            reason: "Message type not allowed");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "Spam"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Reject));
        Assert.That(result.Actions[0].Reason, Is.EqualTo("Message type not allowed"));
    }

    [Test]
    public async Task EvaluateAsync_DeadLetterAction_ReturnsDeadLetterAction()
    {
        var rule = CreateRule("R1", 1, RuleActionType.DeadLetter,
            [new RuleCondition { FieldName = "Source", Operator = RuleConditionOperator.Equals, Value = "UnknownSystem" }],
            reason: "Unknown source system");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(source: "UnknownSystem"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.DeadLetter));
        Assert.That(result.Actions[0].Reason, Is.EqualTo("Unknown source system"));
    }

    [Test]
    public async Task EvaluateAsync_TransformAction_ReturnsTransformAction()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Transform,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "LegacyOrder" }],
            transformName: "legacy-to-modern");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "LegacyOrder"));

        Assert.That(result.HasMatch, Is.True);
        Assert.That(result.Actions[0].ActionType, Is.EqualTo(RuleActionType.Transform));
        Assert.That(result.Actions[0].TransformName, Is.EqualTo("legacy-to-modern"));
    }

    // ------------------------------------------------------------------ //
    // No rules
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_NoRules_ReturnsNoMatch()
    {
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope());

        Assert.That(result.HasMatch, Is.False);
        Assert.That(result.MatchedRules, Is.Empty);
        Assert.That(result.Actions, Is.Empty);
        Assert.That(result.RulesEvaluated, Is.EqualTo(0));
    }

    // ------------------------------------------------------------------ //
    // Empty conditions
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_RuleWithNoConditions_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route, [], targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope());

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Null guard
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = BuildEngine();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync<string>(null!));
    }

    // ------------------------------------------------------------------ //
    // Unknown field returns no match
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_UnknownFieldName_DoesNotMatch()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "UnknownField", Operator = RuleConditionOperator.Equals, Value = "test" }],
            targetTopic: "unknown");
        await _store.AddOrUpdateAsync(rule);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope());

        Assert.That(result.HasMatch, Is.False);
    }

    // ------------------------------------------------------------------ //
    // RulesEvaluated count
    // ------------------------------------------------------------------ //

    [Test]
    public async Task EvaluateAsync_ReportsCorrectRulesEvaluatedCount()
    {
        var rule1 = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "Nomatch" }],
            targetTopic: "none");
        var rule2 = CreateRule("R2", 2, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule1);
        await _store.AddOrUpdateAsync(rule2);
        var sut = BuildEngine();

        var result = await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        Assert.That(result.RulesEvaluated, Is.EqualTo(2));
        Assert.That(result.HasMatch, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_CacheEnabled_DoesNotCallStoreOnSecondEvaluation()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders");
        await _store.AddOrUpdateAsync(rule);

        var storeProxy = Substitute.For<IRuleStore>();
        storeProxy.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BusinessRule>>([rule]));

        var sut = new BusinessRuleEngine(
            storeProxy,
            Options.Create(new RuleEngineOptions { CacheEnabled = true, CacheRefreshIntervalMs = 60_000 }),
            NullLogger<BusinessRuleEngine>.Instance);

        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));
        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        // Only one call to store — second evaluation uses cache.
        await storeProxy.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAsync_CacheDisabled_CallsStoreEveryTime()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders");

        var storeProxy = Substitute.For<IRuleStore>();
        storeProxy.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BusinessRule>>([rule]));

        var sut = new BusinessRuleEngine(
            storeProxy,
            Options.Create(new RuleEngineOptions { CacheEnabled = false }),
            NullLogger<BusinessRuleEngine>.Instance);

        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));
        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        await storeProxy.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAsync_CacheExpired_RefreshesFromStore()
    {
        var rule = CreateRule("R1", 1, RuleActionType.Route,
            [new RuleCondition { FieldName = "MessageType", Operator = RuleConditionOperator.Equals, Value = "OrderCreated" }],
            targetTopic: "orders");

        var storeProxy = Substitute.For<IRuleStore>();
        storeProxy.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BusinessRule>>([rule]));

        var sut = new BusinessRuleEngine(
            storeProxy,
            Options.Create(new RuleEngineOptions { CacheEnabled = true, CacheRefreshIntervalMs = 1 }),
            NullLogger<BusinessRuleEngine>.Instance);

        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));
        await Task.Delay(20); // let cache expire
        await sut.EvaluateAsync(BuildEnvelope(messageType: "OrderCreated"));

        await storeProxy.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
