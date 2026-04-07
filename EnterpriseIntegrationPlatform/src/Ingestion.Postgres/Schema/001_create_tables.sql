-- ============================================================================
-- EIP PostgreSQL Message Broker Schema
-- ============================================================================
-- Supports all EIP patterns: publish/subscribe, point-to-point, competing
-- consumers, dead-letter queues, durable subscriptions, and channel purge.
-- Target: ≤ 5,000 TPS on a standard PostgreSQL instance.
-- ============================================================================

-- Main message store. Every published message becomes a row.
-- Consumer groups consume independently via eip_subscriptions.
CREATE TABLE IF NOT EXISTS eip_messages (
    id              BIGSERIAL       PRIMARY KEY,
    message_id      UUID            NOT NULL,
    topic           TEXT            NOT NULL,
    payload         JSONB           NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),

    -- Indexing for topic-based reads and cleanup
    CONSTRAINT uq_eip_messages_message_id UNIQUE (message_id)
);

CREATE INDEX IF NOT EXISTS ix_eip_messages_topic_id
    ON eip_messages (topic, id);

-- Subscription tracking: one row per (consumer_group, message).
-- Competing consumers use SELECT … FOR UPDATE SKIP LOCKED on unprocessed rows.
CREATE TABLE IF NOT EXISTS eip_subscriptions (
    id              BIGSERIAL       PRIMARY KEY,
    message_id      BIGINT          NOT NULL REFERENCES eip_messages(id) ON DELETE CASCADE,
    topic           TEXT            NOT NULL,
    consumer_group  TEXT            NOT NULL,
    delivered_at    TIMESTAMPTZ,            -- NULL = pending delivery
    locked_until    TIMESTAMPTZ,            -- row-level lock expiry for competing consumers
    locked_by       TEXT,                   -- consumer instance identifier

    CONSTRAINT uq_eip_sub_group_msg UNIQUE (consumer_group, message_id)
);

CREATE INDEX IF NOT EXISTS ix_eip_subscriptions_pending
    ON eip_subscriptions (topic, consumer_group, id)
    WHERE delivered_at IS NULL;

-- Dead-letter table: failed messages after retry exhaustion.
CREATE TABLE IF NOT EXISTS eip_dead_letters (
    id              BIGSERIAL       PRIMARY KEY,
    original_id     BIGINT          REFERENCES eip_messages(id) ON DELETE SET NULL,
    topic           TEXT            NOT NULL,
    consumer_group  TEXT            NOT NULL,
    payload         JSONB           NOT NULL,
    reason          TEXT            NOT NULL,
    error_message   TEXT,
    attempt_count   INT             NOT NULL DEFAULT 0,
    dead_lettered_at TIMESTAMPTZ    NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_eip_dead_letters_topic
    ON eip_dead_letters (topic, dead_lettered_at DESC);

-- Durable subscriber registry: tracks which groups are subscribed to which topics.
-- New messages auto-fan-out to all registered groups.
CREATE TABLE IF NOT EXISTS eip_durable_subscribers (
    id              BIGSERIAL       PRIMARY KEY,
    topic           TEXT            NOT NULL,
    consumer_group  TEXT            NOT NULL,
    registered_at   TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT uq_eip_durable_sub UNIQUE (topic, consumer_group)
);

-- ── pg_notify trigger: low-latency push on new message ──────────────────

CREATE OR REPLACE FUNCTION eip_notify_new_message()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('eip_' || NEW.topic, NEW.id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_eip_notify ON eip_messages;
CREATE TRIGGER trg_eip_notify
    AFTER INSERT ON eip_messages
    FOR EACH ROW
    EXECUTE FUNCTION eip_notify_new_message();

-- ── Fan-out trigger: create subscription rows for all durable subscribers ──

CREATE OR REPLACE FUNCTION eip_fanout_to_subscribers()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO eip_subscriptions (message_id, topic, consumer_group)
    SELECT NEW.id, NEW.topic, ds.consumer_group
    FROM eip_durable_subscribers ds
    WHERE ds.topic = NEW.topic
    ON CONFLICT (consumer_group, message_id) DO NOTHING;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_eip_fanout ON eip_messages;
CREATE TRIGGER trg_eip_fanout
    AFTER INSERT ON eip_messages
    FOR EACH ROW
    EXECUTE FUNCTION eip_fanout_to_subscribers();
