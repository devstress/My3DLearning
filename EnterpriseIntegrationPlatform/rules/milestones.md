# EnterpriseIntegrationPlatform – Milestones

> **To continue development, tell the AI agent:**
>
> ```
> continue next chunk
> ```
>
> The agent will read this file, find the next chunk with status `not-started`,
> implement it, update the status to `done`, update `Next Chunk`, and log details
> in `rules/completion-log.md`.

> **ENFORCEMENT RULE — Completed chunks must be removed from this file.**
>
> When a chunk is marked `done`:
> 1. Log full details (date, goal, architecture, files created/modified, test counts) in `rules/completion-log.md`.
> 2. **Remove the done row from this file.** Milestones.md contains only `not-started` chunks.
> 3. If an entire phase has no remaining rows, replace the table with: `✅ Phase N complete — see completion-log.md`.
> 4. Update the `Next Chunk` section to point to the next `not-started` chunk.
>
> This rule is mandatory for every AI agent session. Never leave done rows in milestones.md.

## Completed Phases

✅ Phases 1–27 complete — see `rules/completion-log.md` for full history.

48 src projects + Ingestion.Postgres = 49 src projects. 522 TutorialLabs tests across 50 tutorials. Broker providers: NATS JetStream, Kafka, Pulsar, **PostgreSQL**.

---

## Phase 28 — PostgreSQL Message Broker (EIP-Complete, ≤ 5k TPS)

**Goal:** Add PostgreSQL as a full-featured EIP message broker for lower-scale deployments (≤ 5,000 TPS).
Many organisations already run Postgres — adding it as a broker eliminates the operational overhead of
a dedicated message system for smaller teams. The implementation must support **all EIP behaviours**
currently available on NATS/Kafka/Pulsar: publish/subscribe, point-to-point, content-based routing,
dead-letter queues, retry with backoff, transactional publish, durable subscriptions, channel purge,
competing consumers, selective consumption, and polling.

**Design:**
- New project `src/Ingestion.Postgres/` (mirrors Ingestion.Nats/Kafka/Pulsar structure)
- `BrokerType.Postgres = 3` added to the existing enum
- Schema: `eip_messages` table (id, topic, consumer_group, payload JSONB, created_at, locked_until, delivered_at, dead_lettered_at) + indexes
- Low-latency delivery via `pg_notify` on INSERT trigger; polling fallback for reliability
- Consumer groups via `SELECT … FOR UPDATE SKIP LOCKED` row locking
- Native `NpgsqlTransaction` for `ITransactionalClient` — true ACID atomicity
- DLQ via `dead_lettered_at` column + `eip_dead_letters` table
- Durable subscriber: rows remain until explicitly ACKed
- Channel purge: `DELETE FROM eip_messages WHERE topic = $1`
- `AddPostgresBroker(services, connectionString)` DI extension following NATS pattern
- Aspire TestAppHost gets a Postgres container for integration tests
- All existing EIP components (routers, DLQ publisher, retry, splitter, aggregator, etc.)
  work unchanged because they depend only on `IMessageBrokerProducer`/`IMessageBrokerConsumer`

**Architecture — why this is an EIP fix, not a test fix:**
The existing `IMessageBrokerProducer` / `IMessageBrokerConsumer` abstractions already decouple
all 48 EIP components from the transport. Adding Postgres as a fourth provider proves the
architecture is sound and gives teams a deployment option that requires zero additional
infrastructure beyond their existing database. All EIP patterns (routing, DLQ, retry,
transactions, channels, competing consumers) work through the same interfaces.

| Chunk | Scope | Status |
|-------|-------|--------|
| 107 | **DLQ + Retry + Channels on Postgres** — Verify `DeadLetterPublisher<T>`, `ExponentialBackoffRetryPolicy`, `InvalidMessageChannel`, `PointToPointChannel`, `PublishSubscribeChannel`, `DatatypeChannel`, `MessagingBridge` all work unchanged with Postgres producer/consumer. Integration tests exercising each EIP pattern end-to-end through Postgres. | `not-started` |
| 108 | **DI wiring + Aspire integration** — `AddPostgresBroker(services, connectionString)` extension. Register in `IngestionServiceExtensions.BrokerRegistrations`. Add Postgres container to `tests/TestAppHost/Program.cs`. `PostgresBrokerEndpoint` test helper (mirrors `NatsBrokerEndpoint`). Connectivity integration tests. | `not-started` |
| 109 | **Routing + advanced EIP on Postgres** — Integration tests: `ContentBasedRouter`, `DynamicRouter`, `RecipientListRouter`, `RoutingSlipRouter`, `MessageFilter`, `Detour`, `ScatterGather`, `Splitter`, `Aggregator`, `Resequencer` — all wired to Postgres broker. Proves every EIP routing pattern works on Postgres transport. | `not-started` |

**Next Chunk:** 107

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.
