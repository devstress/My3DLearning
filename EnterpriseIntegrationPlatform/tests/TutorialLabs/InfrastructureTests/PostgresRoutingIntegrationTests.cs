// ============================================================================
// PostgresRoutingIntegrationTests – Proves EIP routing patterns work on Postgres
// ============================================================================
// These tests wire real EIP routing components (ContentBasedRouter, MessageFilter,
// RecipientListRouter, DynamicRouter, Detour) to the real PostgresBrokerEndpoint.
// Each test creates unique topics to prevent cross-test interference.
// Requires Docker (Aspire Postgres container); tests fail when unavailable.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.InfrastructureTests;

/// <summary>
/// Integration tests proving all EIP routing patterns work when wired to
/// a real PostgreSQL message broker (via <see cref="PostgresBrokerEndpoint"/>).
/// </summary>
[TestFixture]
public sealed class PostgresRoutingIntegrationTests
{
    // ── 1. Content-Based Router on Postgres ──────────────────────────────

    [Test]
    public async Task ContentBasedRouter_RoutesOnMessageType_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("cbr-pg", connStr);

        var ordersOutputTopic = $"orders-{Guid.NewGuid():N}";
        var alertsOutputTopic = $"alerts-{Guid.NewGuid():N}";
        var defaultTopic = $"default-{Guid.NewGuid():N}";

        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                DefaultTopic = defaultTopic,
                Rules =
                [
                    new RoutingRule
                    {
                        Priority = 1,
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "OrderCreated",
                        TargetTopic = ordersOutputTopic,
                        Name = "Orders"
                    },
                    new RoutingRule
                    {
                        Priority = 2,
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "AlertRaised",
                        TargetTopic = alertsOutputTopic,
                        Name = "Alerts"
                    }
                ]
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var orderEnv = IntegrationEnvelope<string>.Create("Order data", "shop", "OrderCreated");
        var alertEnv = IntegrationEnvelope<string>.Create("Alert data", "monitor", "AlertRaised");
        var unknownEnv = IntegrationEnvelope<string>.Create("Unknown data", "other", "Unknown");

        await router.RouteAsync(orderEnv);
        await router.RouteAsync(alertEnv);
        await router.RouteAsync(unknownEnv);

        broker.AssertReceivedOnTopic(ordersOutputTopic, 1);
        broker.AssertReceivedOnTopic(alertsOutputTopic, 1);
        broker.AssertReceivedOnTopic(defaultTopic, 1);
        broker.AssertReceivedCount(3);
    }

    // ── 2. Content-Based Router with Metadata ───────────────────────────

    [Test]
    public async Task ContentBasedRouter_RoutesOnMetadata_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("cbr-meta-pg", connStr);

        var vipTopic = $"vip-{Guid.NewGuid():N}";
        var standardTopic = $"standard-{Guid.NewGuid():N}";

        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                DefaultTopic = standardTopic,
                Rules =
                [
                    new RoutingRule
                    {
                        Priority = 1,
                        FieldName = "Metadata.tier",
                        Operator = RoutingOperator.Equals,
                        Value = "vip",
                        TargetTopic = vipTopic,
                        Name = "VIP tier"
                    }
                ]
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var vipEnv = IntegrationEnvelope<string>.Create("VIP order", "shop", "Order");
        vipEnv.Metadata["tier"] = "vip";

        var stdEnv = IntegrationEnvelope<string>.Create("Standard order", "shop", "Order");
        stdEnv.Metadata["tier"] = "standard";

        await router.RouteAsync(vipEnv);
        await router.RouteAsync(stdEnv);

        broker.AssertReceivedOnTopic(vipTopic, 1);
        broker.AssertReceivedOnTopic(standardTopic, 1);
    }

    // ── 3. Message Filter on Postgres ───────────────────────────────────

    [Test]
    public async Task MessageFilter_FiltersAndRoutes_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("filter-pg", connStr);

        var passTopic = $"passed-{Guid.NewGuid():N}";
        var discardTopic = $"discarded-{Guid.NewGuid():N}";

        var filter = new MessageFilter(
            broker,
            Options.Create(new MessageFilterOptions
            {
                OutputTopic = passTopic,
                DiscardTopic = discardTopic,
                Logic = RuleLogicOperator.And,
                Conditions =
                [
                    new RuleCondition
                    {
                        FieldName = "Source",
                        Operator = RuleConditionOperator.Equals,
                        Value = "trusted"
                    }
                ]
            }),
            NullLogger<MessageFilter>.Instance);

        var trusted = IntegrationEnvelope<string>.Create("From trusted", "trusted", "event");
        var untrusted = IntegrationEnvelope<string>.Create("From untrusted", "rogue", "event");

        await filter.FilterAsync(trusted);
        await filter.FilterAsync(untrusted);

        broker.AssertReceivedOnTopic(passTopic, 1);
        broker.AssertReceivedOnTopic(discardTopic, 1);
        broker.AssertReceivedCount(2);
    }

    // ── 4. Recipient List Router on Postgres ────────────────────────────

    [Test]
    public async Task RecipientListRouter_FansOutToAllMatching_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("rlist-pg", connStr);

        var auditTopic = $"audit-{Guid.NewGuid():N}";
        var billingTopic = $"billing-{Guid.NewGuid():N}";
        var notifyTopic = $"notify-{Guid.NewGuid():N}";

        var router = new RecipientListRouter(
            broker,
            Options.Create(new RecipientListOptions
            {
                Rules =
                [
                    new RecipientListRule
                    {
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Equals,
                        Value = "OrderCompleted",
                        Destinations = [auditTopic, billingTopic],
                        Name = "Order completion"
                    },
                    new RecipientListRule
                    {
                        FieldName = "Source",
                        Operator = RoutingOperator.Equals,
                        Value = "shop",
                        Destinations = [notifyTopic],
                        Name = "Shop notification"
                    }
                ]
            }),
            NullLogger<RecipientListRouter>.Instance);

        var env = IntegrationEnvelope<string>.Create("Order done", "shop", "OrderCompleted");
        await router.RouteAsync(env);

        // Both rules match: audit + billing + notify = 3 publications
        broker.AssertReceivedOnTopic(auditTopic, 1);
        broker.AssertReceivedOnTopic(billingTopic, 1);
        broker.AssertReceivedOnTopic(notifyTopic, 1);
        broker.AssertReceivedCount(3);
    }

    // ── 5. Dynamic Router on Postgres ───────────────────────────────────

    [Test]
    public async Task DynamicRouter_RegisterAndRoute_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("dynroute-pg", connStr);

        var fallbackTopic = $"fallback-{Guid.NewGuid():N}";
        var ordersTopic = $"dyn-orders-{Guid.NewGuid():N}";

        var router = new DynamicRouter(
            broker,
            Options.Create(new DynamicRouterOptions
            {
                ConditionField = "MessageType",
                FallbackTopic = fallbackTopic,
                CaseInsensitive = true,
            }),
            NullLogger<DynamicRouter>.Instance);

        // Initially no routes registered: falls back
        var env1 = IntegrationEnvelope<string>.Create("pre-register", "shop", "OrderCreated");
        await router.RouteAsync(env1);
        broker.AssertReceivedOnTopic(fallbackTopic, 1);

        // Register a route dynamically
        await router.RegisterAsync("OrderCreated", ordersTopic);

        var env2 = IntegrationEnvelope<string>.Create("post-register", "shop", "OrderCreated");
        await router.RouteAsync(env2);
        broker.AssertReceivedOnTopic(ordersTopic, 1);

        broker.AssertReceivedCount(2);
    }

    // ── 6. Detour on Postgres ───────────────────────────────────────────

    [Test]
    public async Task Detour_ActivateAndDeactivate_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("detour-pg", connStr);

        var normalTopic = $"normal-{Guid.NewGuid():N}";
        var detourTopic = $"detour-{Guid.NewGuid():N}";

        var detour = new Detour(
            broker,
            Options.Create(new DetourOptions
            {
                OutputTopic = normalTopic,
                DetourTopic = detourTopic,
                EnabledAtStartup = false,
            }),
            NullLogger<Detour>.Instance);

        // Normal routing (detour off)
        var env1 = IntegrationEnvelope<string>.Create("normal msg", "app", "event");
        await detour.RouteAsync(env1);
        broker.AssertReceivedOnTopic(normalTopic, 1);

        // Activate detour
        detour.SetEnabled(true);

        var env2 = IntegrationEnvelope<string>.Create("detoured msg", "app", "event");
        await detour.RouteAsync(env2);
        broker.AssertReceivedOnTopic(detourTopic, 1);

        // Deactivate detour
        detour.SetEnabled(false);

        var env3 = IntegrationEnvelope<string>.Create("back to normal", "app", "event");
        await detour.RouteAsync(env3);
        broker.AssertReceivedOnTopic(normalTopic, 2);

        broker.AssertReceivedCount(3);
    }

    // ── 7. Content-Based Router with Regex on Postgres ──────────────────

    [Test]
    public async Task ContentBasedRouter_RegexRouting_ViaPostgres()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null) { Assert.Fail("Docker not available"); return; }

        await using var broker = new PostgresBrokerEndpoint("cbr-regex-pg", connStr);

        var matchTopic = $"regex-match-{Guid.NewGuid():N}";
        var defaultTopic = $"regex-default-{Guid.NewGuid():N}";

        var router = new ContentBasedRouter(
            broker,
            Options.Create(new RouterOptions
            {
                DefaultTopic = defaultTopic,
                Rules =
                [
                    new RoutingRule
                    {
                        Priority = 1,
                        FieldName = "MessageType",
                        Operator = RoutingOperator.Regex,
                        Value = "^Order.*",
                        TargetTopic = matchTopic,
                        Name = "Order regex"
                    }
                ]
            }),
            NullLogger<ContentBasedRouter>.Instance);

        var orderCreated = IntegrationEnvelope<string>.Create("data", "shop", "OrderCreated");
        var orderCancelled = IntegrationEnvelope<string>.Create("data", "shop", "OrderCancelled");
        var paymentReceived = IntegrationEnvelope<string>.Create("data", "shop", "PaymentReceived");

        await router.RouteAsync(orderCreated);
        await router.RouteAsync(orderCancelled);
        await router.RouteAsync(paymentReceived);

        broker.AssertReceivedOnTopic(matchTopic, 2);
        broker.AssertReceivedOnTopic(defaultTopic, 1);
    }
}
