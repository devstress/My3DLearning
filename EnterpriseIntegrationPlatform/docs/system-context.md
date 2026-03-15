# System Context (C4 Model — Level 1)

## Overview

This document describes the system context of the Enterprise Integration Platform using the C4 model. It identifies the external actors that interact with the platform, the platform itself as the central system, and the external infrastructure dependencies it relies upon.

## System Context Diagram

```
                    ┌─────────────────┐
                    │  Admin Users    │
                    │  (Operations)   │
                    └────────┬────────┘
                             │ Manages routes, monitors
                             │ health, reviews DLQ
                             ▼
┌──────────────┐    ┌────────────────────────┐    ┌──────────────────┐
│ API          │    │                        │    │  Target Systems  │
│ Consumers   │───▶│   Enterprise           │───▶│                  │
│ (Upstream)   │    │   Integration          │    │  • REST APIs     │
└──────────────┘    │   Platform             │    │  • SFTP Servers  │
                    │                        │    │  • Email (SMTP)  │
┌──────────────┐    │  Receives, transforms, │    │  • File Systems  │
│ SFTP         │───▶│  routes, and delivers  │    └──────────────────┘
│ Servers      │    │  integration messages  │
└──────────────┘    │                        │
                    │                        │
┌──────────────┐    └───────────┬────────────┘
│ Email        │───▶            │
│ Systems      │     ┌──────────┼──────────────────────────┐
└──────────────┘     │          │                          │
                     ▼          ▼                          ▼
┌──────────────┐  ┌──────────────────┐  ┌───────────┐  ┌────────┐
│ Apache Kafka │  │  Temporal.io     │  │ Cassandra │  │ Ollama │
│ (Events)     │  │  (Workflows)     │  │ (Storage) │  │ (AI)   │
└──────────────┘  └──────────────────┘  └───────────┘  └────────┘
```

## External Actors

### API Consumers (Upstream Systems)

**Type:** External software systems

API consumers are upstream applications that send messages to the platform via HTTP/REST endpoints. These include:

- Line-of-business applications submitting integration requests
- Partner systems sending webhooks for event notifications
- Scheduled batch processes pushing data files via API

**Interaction:** Sends HTTP POST requests containing payloads (JSON, XML, binary) to the platform's ingress API. Receives synchronous acknowledgment (202 Accepted) with a correlation ID for tracking.

### SFTP Servers

**Type:** External file transfer systems

Remote SFTP servers host files that the platform polls for and retrieves on configurable schedules.

**Interaction:** The platform connects to SFTP servers using SSH key or credential-based authentication, lists directory contents, downloads new files, and optionally archives or deletes processed files.

### Email Systems

**Type:** External email infrastructure

Email systems (IMAP/POP3 mailboxes) provide inbound messages and attachments for processing.

**Interaction:** The platform monitors configured mailboxes, retrieves new emails, extracts attachments, and creates integration envelopes from the email content and metadata.

### File Systems

**Type:** Local or network-attached storage

File systems (local directories, NFS mounts, SMB shares) host files for ingestion.

**Interaction:** File system watchers detect new files via polling or OS-level notifications, read file contents, and create integration envelopes. Processed files are moved to archive directories.

### Admin Users (Operations)

**Type:** Human actors

Operations and support personnel who manage and monitor the platform.

**Interaction:** Admin users access the Admin API and monitoring dashboards to:
- Configure integration routes and transformation rules
- Monitor message processing throughput and error rates
- Review and replay messages from dead letter queues
- Manage tenant configurations and access policies
- View distributed traces for troubleshooting

## The System: Enterprise Integration Platform

**Purpose:** Receive messages from diverse sources, apply transformations and routing logic, and reliably deliver them to target systems.

**Key Responsibilities:**
- Multi-protocol message ingestion (HTTP, SFTP, Email, File)
- Message normalization into a canonical envelope format
- Content-based routing and recipient list resolution
- Message transformation (format conversion, mapping, enrichment)
- Durable workflow orchestration for multi-step processing
- Reliable delivery to target systems with retry and compensation
- Full observability with distributed tracing and metrics
- RAG-powered knowledge retrieval for integration development

## External Dependencies

### Apache Kafka

**Role:** Event streaming for broadcast, audit, and analytics

Kafka provides the event streaming layer for broadcast workloads, audit log streaming, and fan-out analytics. Task-oriented message delivery (ingestion, routing, DLQ) uses the configurable queue broker (NATS JetStream or Apache Pulsar) to avoid per-partition serialization (head-of-line blocking).

**Dependency Type:** Critical for streaming workloads — task delivery falls back to queue broker.

### Queue Broker (NATS JetStream / Apache Pulsar)

**Role:** Task-oriented message delivery with no head-of-line blocking

NATS JetStream (default for local development and cloud) or Apache Pulsar with Key_Shared subscription (switchable for large-scale production) handles ingestion, delivery, and DLQ processing. Per-subject queue groups (NATS) or Key_Shared distribution by recipientId (Pulsar) ensure that Recipient A never blocks Recipient B, even at 1 million recipients.

**Dependency Type:** Critical — platform cannot process task delivery without the queue broker.

### Temporal.io

**Role:** Durable workflow orchestration engine

Temporal manages the lifecycle of integration workflows, providing durability guarantees, retry policies, and saga coordination. It persists workflow state independently of the application services.

**Dependency Type:** Critical — workflows cannot execute without Temporal.

### Apache Cassandra

**Role:** Distributed data store

Cassandra stores message payloads, deduplication keys, workflow metadata, and audit logs. Its masterless architecture provides high availability and horizontal scalability.

**Dependency Type:** Critical — message persistence and deduplication require Cassandra.

### Ollama

**Role:** Local AI runtime for RAG retrieval within RagFlow

Ollama provides embedding models that power RagFlow's knowledge base retrieval. The platform's docs, rules, and source code are indexed via RagFlow, and Ollama generates the embeddings for similarity search. Developers use their own preferred AI provider (Copilot, Codex, Claude Code) for code generation, connecting to the self-hosted RAG API for platform context.

**Dependency Type:** Optional — the platform operates fully without Ollama. RAG retrieval features degrade gracefully when unavailable.

## Communication Protocols

| From              | To                 | Protocol    | Authentication        |
|-------------------|--------------------|-------------|-----------------------|
| API Consumers     | Ingress API        | HTTPS       | OAuth 2.0 / API Key   |
| Platform          | SFTP Servers       | SFTP/SSH    | SSH Keys / Credentials |
| Platform          | Email Systems      | IMAPS/POP3S | Credentials            |
| Platform          | Target REST APIs   | HTTPS       | OAuth 2.0 / API Key   |
| Platform          | Target SFTP        | SFTP/SSH    | SSH Keys / Credentials |
| Platform          | Kafka              | TCP/TLS     | SASL/mTLS             |
| Platform          | NATS/Pulsar        | TCP/TLS     | Token/mTLS            |
| Platform          | Temporal           | gRPC/TLS    | mTLS / API Key        |
| Platform          | Cassandra          | CQL/TLS     | Credentials / mTLS    |
| Platform          | Ollama             | HTTP        | Local only (no auth)  |
| Admin Users       | Admin API          | HTTPS       | OAuth 2.0 / JWT       |
