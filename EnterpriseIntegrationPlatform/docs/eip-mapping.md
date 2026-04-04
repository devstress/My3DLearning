# Enterprise Integration Patterns — Complete Reference

> Full mapping of all 65 Enterprise Integration Patterns (Hohpe & Woolf, 2003)
> to their implementations in the Enterprise Integration Platform.
>
> Reference: *Enterprise Integration Patterns: Designing, Building, and Deploying
> Messaging Solutions* — Gregor Hohpe & Bobby Woolf (Addison-Wesley, 2003).

---

## Quick Reference

| # | Pattern | Category | Platform Component | Project / Namespace |
|---|---------|----------|-------------------|---------------------|
| 1 | File Transfer | Integration Styles | Connector.File | `src/Connector.File` |
| 2 | Shared Database | Integration Styles | Storage.Cassandra | `src/Storage.Cassandra` |
| 3 | Remote Procedure Invocation | Integration Styles | Connector.Http | `src/Connector.Http` |
| 4 | Messaging | Integration Styles | Core Architecture | Platform-wide |
| 5 | Message Channel | Messaging Systems | Ingestion broker abstraction | `src/Ingestion` |
| 6 | Message | Messaging Systems | IntegrationEnvelope | `src/Contracts` |
| 7 | Pipes and Filters | Messaging Systems | Temporal activity chains | `src/Workflow.Temporal` + `src/Processing.Transform` |
| 8 | Message Router | Messaging Systems | Processing.Routing | `src/Processing.Routing` |
| 9 | Message Translator | Messaging Systems | Processing.Translator + Transform | `src/Processing.Translator` + `src/Processing.Transform` |
| 10 | Message Endpoint | Messaging Systems | Ingestion consumers | `src/Ingestion` |
| 11 | Point-to-Point Channel | Messaging Channels | PointToPointChannel | `src/Ingestion` (Channels) |
| 12 | Publish-Subscribe Channel | Messaging Channels | PublishSubscribeChannel | `src/Ingestion` (Channels) |
| 13 | Datatype Channel | Messaging Channels | DatatypeChannel | `src/Ingestion` (Channels) |
| 14 | Invalid Message Channel | Messaging Channels | InvalidMessageChannel | `src/Ingestion` (Channels) |
| 15 | Dead Letter Channel | Messaging Channels | Processing.DeadLetter | `src/Processing.DeadLetter` |
| 16 | Guaranteed Delivery | Messaging Channels | Kafka + Temporal | Infrastructure |
| 17 | Channel Adapter | Messaging Channels | Connector.* | `src/Connector.Http`, `.Sftp`, `.Email`, `.File` |
| 18 | Messaging Bridge | Messaging Channels | MessagingBridge | `src/Ingestion` (Channels) |
| 19 | Message Bus | Messaging Channels | The platform itself | Platform-wide |
| 20 | Command Message | Message Construction | IntegrationEnvelope.Intent = Command | `src/Contracts` |
| 21 | Document Message | Message Construction | IntegrationEnvelope.Intent = Document | `src/Contracts` |
| 22 | Event Message | Message Construction | IntegrationEnvelope.Intent = Event | `src/Contracts` |
| 23 | Request-Reply | Message Construction | RequestReplyCorrelator | `src/Processing.RequestReply` |
| 24 | Return Address | Message Construction | IntegrationEnvelope.ReplyTo | `src/Contracts` |
| 25 | Correlation Identifier | Message Construction | IntegrationEnvelope.CorrelationId | `src/Contracts` |
| 26 | Message Sequence | Message Construction | SequenceNumber / TotalCount | `src/Contracts` |
| 27 | Message Expiration | Message Construction | ExpiresAt + MessageExpirationChecker | `src/Contracts` |
| 28 | Format Indicator | Message Construction | MessageHeaders.ContentType | `src/Contracts` |
| 29 | Content-Based Router | Message Routing | ContentBasedRouter | `src/Processing.Routing` |
| 30 | Message Filter | Message Routing | MessageFilter | `src/Processing.Routing` |
| 31 | Dynamic Router | Message Routing | DynamicRouter | `src/Processing.Routing` |
| 32 | Recipient List | Message Routing | RecipientListRouter | `src/Processing.Routing` |
| 33 | Splitter | Message Routing | Processing.Splitter | `src/Processing.Splitter` |
| 34 | Aggregator | Message Routing | Processing.Aggregator | `src/Processing.Aggregator` |
| 35 | Resequencer | Message Routing | MessageResequencer | `src/Processing.Resequencer` |
| 36 | Composed Message Processor | Message Routing | Splitter + Transform + Aggregator | Pipeline composition |
| 37 | Scatter-Gather | Message Routing | Processing.ScatterGather | `src/Processing.ScatterGather` |
| 38 | Routing Slip | Message Routing | RoutingSlipRouter | `src/Processing.Routing` |
| 39 | Process Manager | Message Routing | Temporal Workflows | `src/Workflow.Temporal` |
| 40 | Message Broker | Message Routing | The platform itself | Platform-wide |
| 41 | Envelope Wrapper | Message Transformation | IntegrationEnvelope | `src/Contracts` |
| 42 | Content Enricher | Message Transformation | ContentEnricher | `src/Processing.Transform` |
| 43 | Content Filter | Message Transformation | ContentFilter | `src/Processing.Transform` |
| 44 | Claim Check | Message Transformation | Storage.Cassandra | `src/Storage.Cassandra` |
| 45 | Normalizer | Message Transformation | MessageNormalizer | `src/Processing.Transform` |
| 46 | Canonical Data Model | Message Transformation | IntegrationEnvelope\<T\> | `src/Contracts` |
| 47 | Messaging Gateway | Messaging Endpoints | IMessagingGateway + HttpMessagingGateway | `src/Gateway.Api` |
| 48 | Messaging Mapper | Messaging Endpoints | IMessagingMapper + JsonMessagingMapper | `src/Contracts` |
| 49 | Transactional Client | Messaging Endpoints | ITransactionalClient + BrokerTransactionalClient | `src/Ingestion` |
| 50 | Polling Consumer | Messaging Endpoints | IPollingConsumer + PollingConsumer | `src/Ingestion` |
| 51 | Event-Driven Consumer | Messaging Endpoints | IEventDrivenConsumer + EventDrivenConsumer | `src/Ingestion` |
| 52 | Competing Consumers | Messaging Endpoints | Processing.CompetingConsumers | `src/Processing.CompetingConsumers` |
| 53 | Message Dispatcher | Messaging Endpoints | MessageDispatcher | `src/Processing.Dispatcher` |
| 54 | Selective Consumer | Messaging Endpoints | ISelectiveConsumer + SelectiveConsumer | `src/Ingestion` |
| 55 | Durable Subscriber | Messaging Endpoints | IDurableSubscriber + DurableSubscriber | `src/Ingestion` |
| 56 | Idempotent Receiver | Messaging Endpoints | Storage.Cassandra dedup | `src/Storage.Cassandra` |
| 57 | Service Activator | Messaging Endpoints | ServiceActivator | `src/Processing.Dispatcher` |
| 58 | Control Bus | System Management | ControlBusPublisher | `src/SystemManagement` |
| 59 | Detour | System Management | Detour | `src/Processing.Routing` |
| 60 | Wire Tap | System Management | OpenTelemetry / Observability | `src/Observability` |
| 61 | Message History | System Management | MessageHistoryHelper | `src/Contracts` |
| 62 | Message Store | System Management | MessageStore | `src/SystemManagement` |
| 63 | Smart Proxy | System Management | SmartProxy | `src/SystemManagement` |
| 64 | Test Message | System Management | TestMessageGenerator | `src/SystemManagement` |
| 65 | Channel Purger | System Management | ChannelPurger | `src/Ingestion` |

---

## 1. Integration Styles

### 1.1 File Transfer

**Book definition**: Applications share data by producing and consuming files.

**Implementation**: `Connector.File` (`src/Connector.File`) implements the `IFileConnector` interface for reading and writing files from local, NFS, or SMB paths. Files are written atomically (write-to-temp then rename) to prevent partial reads.

**Usage**: A Temporal activity writes the transformed `IntegrationEnvelope` payload to a configured file path. The connector supports configurable encoding (UTF-8, UTF-16, ASCII) and path templates with timestamp/message-ID placeholders.

### 1.2 Shared Database

**Book definition**: Applications share data through a common database.

**Implementation**: `Storage.Cassandra` (`src/Storage.Cassandra`) provides the shared persistence layer. CassandraDB with replication factor 3 stores message records, event-sourced state, snapshots, and deduplication entries. All services read from and write to the same Cassandra cluster.

**Usage**: The `IMessageStateStore` and `IEventStore` interfaces abstract Cassandra access. The `MessageRecord` table stores message lifecycle data. Event sourcing captures state transitions as an append-only log with periodic snapshots for fast replay.

### 1.3 Remote Procedure Invocation

**Book definition**: Applications communicate by calling remote procedures.

**Implementation**: `Connector.Http` (`src/Connector.Http`) implements the `IHttpConnector` interface for REST API calls. Supports OAuth 2.0 (with token caching and expiry), Bearer tokens, API keys, and client certificate authentication.

**Usage**: A delivery activity calls `IHttpConnector.SendAsync()` with the envelope payload and target URL. The connector handles authentication, serialization, retries, and response validation. Token caching avoids redundant token requests.

### 1.4 Messaging

**Book definition**: Applications communicate by sending messages through channels.

**Implementation**: The entire platform architecture is built on messaging. Messages flow from `Gateway.Api` through configured brokers (NATS JetStream / Kafka / Pulsar) via `Ingestion`, through processing pipelines orchestrated by `Workflow.Temporal`, and out through `Connector.*` adapters.

---

## 2. Messaging Systems

### 2.1 Message Channel

**Book definition**: Connect the applications using a Message Channel.

**Implementation**: The `Ingestion` project (`src/Ingestion`) provides the broker abstraction layer with `IBrokerProducer` and `IBrokerConsumer` interfaces. Three broker implementations (`Ingestion.Kafka`, `Ingestion.Nats`, `Ingestion.Pulsar`) provide concrete channels.

**Usage**: Channels are created per message type and tenant. NATS JetStream subjects provide per-subject independence without head-of-line blocking. Kafka partitioned topics provide ordered event streams. Pulsar Key_Shared subscriptions distribute by recipient key.

### 2.2 Message

**Book definition**: Package the information into a Message.

**Implementation**: `IntegrationEnvelope<T>` (`src/Contracts`) is the canonical message type. It carries:
- `MessageId` (GUID) — unique identifier
- `CorrelationId` (GUID) — links related messages
- `CausationId` (GUID) — identifies the cause
- `MessageType` (string) — logical type name
- `Payload` (T) — the actual data
- `Metadata` (Dictionary) — extensible headers
- `Timestamp`, `Source`, `Priority`, `Intent`

### 2.3 Pipes and Filters

**Book definition**: Perform complex processing by dividing the task into independent steps connected by channels.

**Implementation**: Temporal activity chains in `Workflow.Temporal` (`src/Workflow.Temporal`) compose processing steps as independent activities. Each activity (validate, transform, route, enrich, deliver) is a self-contained filter connected by the Temporal workflow engine.

**Usage**: The `AtomicPipelineWorkflow` chains activities: Validate → Transform → Route → Deliver. Each activity receives an `IntegrationEnvelope`, processes it, and returns the result to the next step. Processing.Transform provides reusable filter activities (ContentEnricher, ContentFilter, MessageNormalizer).

### 2.4 Message Router

**Book definition**: Insert a special filter that consumes a message from one channel and republishes to another based on conditions.

**Implementation**: `Processing.Routing` (`src/Processing.Routing`) provides `ContentBasedRouter`, `DynamicRouter`, `RecipientListRouter`, `RoutingSlipRouter`, and `MessageFilter`. Each implements `IMessageRouter` and evaluates routing rules against the envelope.

**Usage**: The `ContentBasedRouter` evaluates a list of `RoutingRule` objects (field path, operator, value, destination) against the envelope. The first matching rule determines the output channel. A default route handles unmatched messages.

### 2.5 Message Translator

**Book definition**: Use a special filter to translate one data format into another.

**Implementation**: `Processing.Translator` (`src/Processing.Translator`) handles format conversion (JSON↔XML, CSV→JSON). `Processing.Transform` (`src/Processing.Transform`) provides content enrichment, filtering, and normalization.

**Usage**: Translator activities receive an `IntegrationEnvelope` with the source format, apply mapping rules, and return a new envelope with the translated payload. Schema mapping supports field renaming, type conversion, and structural transformation.

### 2.6 Message Endpoint

**Book definition**: Connect an application to a messaging channel.

**Implementation**: `Ingestion` (`src/Ingestion`) formalizes four endpoint types:
- `IPollingConsumer` / `PollingConsumer` — periodic pull-based consumption
- `IEventDrivenConsumer` / `EventDrivenConsumer` — push-based reactive consumption
- `ISelectiveConsumer` / `SelectiveConsumer` — filter-based selective consumption
- `IDurableSubscriber` / `DurableSubscriber` — persistent subscription with replay

---

## 3. Messaging Channels

### 3.1 Point-to-Point Channel

**Book definition**: Ensure that only one receiver consumes any given message.

**Implementation**: `PointToPointChannel` in `Ingestion.Channels` wraps the broker abstraction to enforce single-consumer semantics. In NATS, this maps to queue groups. In Kafka, single-partition consumer groups. In Pulsar, Exclusive subscriptions.

### 3.2 Publish-Subscribe Channel

**Book definition**: Broadcast an event to all interested receivers.

**Implementation**: `PublishSubscribeChannel` in `Ingestion.Channels` wraps the broker abstraction for fan-out delivery. In NATS, each subscriber receives every message on the subject. In Kafka, each consumer group independently reads the topic. In Pulsar, Shared subscriptions.

### 3.3 Datatype Channel

**Book definition**: Use a separate channel for each data type.

**Implementation**: `DatatypeChannel` in `Ingestion.Channels` enforces that each channel carries only one message type. Channel names include the message type (e.g., `orders.OrderCreated`, `shipping.ShipmentTracking`). The channel validates that published envelopes match the declared type.

### 3.4 Invalid Message Channel

**Book definition**: Route messages that cannot be processed to a special channel.

**Implementation**: `InvalidMessageChannel` in `Ingestion.Channels` receives messages that fail schema validation or cannot be deserialized. Invalid messages are captured with the original bytes and error details for diagnostic inspection.

### 3.5 Dead Letter Channel

**Book definition**: Route messages that cannot be delivered to a Dead Letter Channel.

**Implementation**: `Processing.DeadLetter` (`src/Processing.DeadLetter`) manages the DLQ. When a message exhausts its retry budget, the workflow creates a `FaultEnvelope` containing the original envelope, error details (exception type, message, stack trace), processing history, and retry metadata. The Admin API provides inspect, replay, and discard operations.

### 3.6 Guaranteed Delivery

**Book definition**: Ensure that a message will be delivered even if the messaging system fails.

**Implementation**: Kafka durability (replicated partitions, acks=all) combined with Temporal durable execution ensures no message is lost. NATS JetStream provides at-least-once delivery with server-side acknowledgement. Pulsar provides message persistence with configurable replication.

### 3.7 Channel Adapter

**Book definition**: Connect an application that does not use messaging to a messaging system.

**Implementation**: Four connector projects implement channel adapters:
- `Connector.Http` — REST API adapter with authentication
- `Connector.Sftp` — SFTP file transfer adapter with SSH key auth
- `Connector.Email` — SMTP/SMTPS email adapter with MailKit
- `Connector.File` — Local/NFS/SMB file adapter with atomic writes

Each connector implements `IConnector` from the unified `Connectors` project and is registered via `IConnectorRegistry`.

### 3.8 Messaging Bridge

**Book definition**: Connect multiple messaging systems using a Messaging Bridge.

**Implementation**: `MessagingBridge` in `Ingestion.Channels` bridges messages between different broker implementations. A bridge consumes from one broker (e.g., Kafka) and publishes to another (e.g., NATS JetStream), preserving envelope metadata and correlation context.

### 3.9 Message Bus

**Book definition**: An architecture style where a shared set of channels forms a common communication backbone.

**Implementation**: The Enterprise Integration Platform itself is the message bus. All integrations publish and consume through the shared broker infrastructure. The bus provides routing, transformation, and delivery as platform services rather than application-level concerns.

---

## 4. Message Construction

### 4.1 Command Message

**Book definition**: Use a Command Message to invoke a procedure in another application.

**Implementation**: `IntegrationEnvelope.Intent = MessageIntent.Command`. Command messages carry an explicit action request (e.g., "PlaceOrder", "ShipItem"). The `MessageIntent` enum distinguishes Command, Document, and Event intents.

### 4.2 Document Message

**Book definition**: Use a Document Message to reliably transfer a data structure.

**Implementation**: `IntegrationEnvelope.Intent = MessageIntent.Document`. Document messages carry structured data payloads (order records, invoice data) without implying a specific action. The receiver determines how to process the document.

### 4.3 Event Message

**Book definition**: Use an Event Message for reliable, asynchronous event notification.

**Implementation**: `IntegrationEnvelope.Intent = MessageIntent.Event`. Event messages notify subscribers of state changes (e.g., "OrderCreated", "PaymentProcessed"). Events are published to Publish-Subscribe channels for fan-out.

### 4.4 Request-Reply

**Book definition**: Send a pair of Request-Reply messages, each on its own channel.

**Implementation**: `Processing.RequestReply` (`src/Processing.RequestReply`) provides `RequestReplyCorrelator` which matches request envelopes to reply envelopes using `CorrelationId`. The request envelope's `ReplyTo` field specifies the reply channel.

### 4.5 Return Address

**Book definition**: The request message should contain a Return Address that indicates where the reply should be sent.

**Implementation**: `IntegrationEnvelope.ReplyTo` carries the reply channel name. When a processing step needs to send a response, it publishes to the channel specified in `ReplyTo`. The `RequestReplyCorrelator` uses this field to route replies.

### 4.6 Correlation Identifier

**Book definition**: Each reply message should contain a Correlation Identifier — a unique identifier that indicates which request message this reply is for.

**Implementation**: `IntegrationEnvelope.CorrelationId` (GUID) links all related messages across the pipeline. The Splitter preserves the parent's `CorrelationId` on child messages. The Aggregator groups by `CorrelationId`. Request-Reply matching uses `CorrelationId`.

### 4.7 Message Sequence

**Book definition**: Whenever a large set of data needs to be broken into message-size chunks, send as a Message Sequence with sequence identifiers.

**Implementation**: `IntegrationEnvelope.SequenceNumber` and `IntegrationEnvelope.TotalCount` track position within a sequence. The Splitter sets these fields on each child message. The Aggregator and Resequencer use them to reassemble or reorder sequences.

### 4.8 Message Expiration

**Book definition**: Set the Message Expiration to indicate when a message should be considered stale.

**Implementation**: `IntegrationEnvelope.ExpiresAt` (DateTimeOffset) carries the expiration timestamp. `MessageExpirationChecker` in Contracts validates expiration before processing. Expired messages are routed to the Invalid Message Channel. Kafka topic retention policies enforce broker-level expiration.

### 4.9 Format Indicator

**Book definition**: Include a Format Indicator in the message that describes the format of the data.

**Implementation**: `MessageHeaders.ContentType` in the envelope's `Metadata` dictionary carries the MIME type (e.g., `application/json`, `application/xml`, `text/csv`). The Message Translator uses this header to select the appropriate transformation strategy.

---

## 5. Message Routing

### 5.1 Content-Based Router

**Book definition**: Route each message to the correct recipient based on message content.

**Implementation**: `ContentBasedRouter` in `Processing.Routing` evaluates a list of `RoutingRule` objects against the envelope. Each rule specifies a JSON path, comparison operator, expected value, and destination channel. Rules are evaluated in priority order; the first match determines the route.

### 5.2 Message Filter

**Book definition**: Use a special kind of Message Router that eliminates undesired messages based on criteria.

**Implementation**: `MessageFilter` in `Processing.Routing` evaluates predicate expressions against the envelope. Messages matching the filter criteria are forwarded; non-matching messages are silently discarded (or routed to the Invalid Message Channel if configured).

### 5.3 Dynamic Router

**Book definition**: Route a message to a list of dynamically specified recipients.

**Implementation**: `DynamicRouter` in `Processing.Routing` maintains a routing table that can be updated at runtime via the Control Bus. Routing decisions are made by querying the current table rather than using static rules, enabling real-time reconfiguration without redeployment.

### 5.4 Recipient List

**Book definition**: Route a message to a list of dynamically specified recipients.

**Implementation**: `RecipientListRouter` in `Processing.Routing` resolves a list of target channels from the envelope metadata and routing configuration. The message is published to all resolved channels. Each recipient receives an independent copy of the envelope.

### 5.5 Splitter

**Book definition**: Break out a single message into a sequence of individual messages.

**Implementation**: `Processing.Splitter` (`src/Processing.Splitter`) breaks a batch envelope into individual envelopes. Each child envelope shares the parent's `CorrelationId` and receives a unique `SequenceNumber` (1-based) and the parent's `TotalCount`. Child envelopes are published to the configured broker.

### 5.6 Aggregator

**Book definition**: Combine the results of individual but related messages into a single message.

**Implementation**: `Processing.Aggregator` (`src/Processing.Aggregator`) collects envelopes by `CorrelationId` and waits for the expected count (from the `TotalCount` header). Once all parts arrive, the aggregator produces a combined output envelope. Configurable timeout handles missing parts.

### 5.7 Resequencer

**Book definition**: Collect and re-order messages so that they can be published in a specified order.

**Implementation**: `MessageResequencer` in `Processing.Resequencer` (`src/Processing.Resequencer`) buffers incoming envelopes and releases them in `SequenceNumber` order. It handles gaps (missing sequence numbers) with configurable timeout before releasing partial sequences.

### 5.8 Composed Message Processor

**Book definition**: Maintain the overall message flow when processing a message that consists of multiple elements.

**Implementation**: A Temporal workflow chains Splitter → per-item Transform → Aggregator. The Splitter breaks the batch into individual items, each is transformed independently as a parallel activity, and the Aggregator reassembles the results by `CorrelationId`.

### 5.9 Scatter-Gather

**Book definition**: Route a request message to multiple recipients and re-aggregate the responses.

**Implementation**: `Processing.ScatterGather` (`src/Processing.ScatterGather`) publishes a request to multiple recipient channels concurrently, collects all responses within a configurable timeout window, and aggregates them into a single result envelope. Partial responses are supported.

### 5.10 Routing Slip

**Book definition**: Attach a Routing Slip to each message, specifying the sequence of processing steps.

**Implementation**: `RoutingSlipRouter` in `Processing.Routing` reads a `RoutingSlip` from the envelope metadata — an ordered list of processing step names. After each step completes, the router advances to the next step in the slip. Completed steps are marked in the slip for audit.

### 5.11 Process Manager

**Book definition**: Use a central processing unit to maintain the state of the sequence and determine the next processing step.

**Implementation**: Temporal Workflows (`src/Workflow.Temporal`) provide durable, stateful orchestration. The `AtomicPipelineWorkflow` coordinates multi-step processing (validate → transform → route → deliver) with saga compensation on failure. Workflows survive process restarts and can pause for signals.

### 5.12 Message Broker

**Book definition**: Use a central Message Broker that can receive messages from multiple destinations, determine the correct destination, and route the message to the correct channel.

**Implementation**: The Enterprise Integration Platform itself is the message broker. The Gateway API receives messages, the routing layer determines destinations, and the processing pipeline delivers to the correct output channels. This is an architectural pattern — the platform is the broker.

---

## 6. Message Transformation

### 6.1 Envelope Wrapper

**Book definition**: Wrap application data in an envelope with messaging-specific headers.

**Implementation**: `IntegrationEnvelope<T>` wraps every payload with standardized metadata: MessageId, CorrelationId, CausationId, MessageType, Source, Priority, Timestamp, Intent, ReplyTo, SequenceNumber, TotalCount, ExpiresAt, and extensible Metadata dictionary.

### 6.2 Content Enricher

**Book definition**: Use a specialized transformer to access an external data source to augment a message.

**Implementation**: `ContentEnricher` in `Processing.Transform` (`src/Processing.Transform`) augments envelope payloads with data from external sources. The enricher calls registered data sources (HTTP APIs, databases, caches) and merges the retrieved data into the envelope payload or metadata.

### 6.3 Content Filter

**Book definition**: Use a Content Filter to remove unimportant data items from a message.

**Implementation**: `ContentFilter` in `Processing.Transform` removes specified fields or sections from the envelope payload. Filter rules specify JSON paths to remove, whitelist-only fields to keep, or regex patterns to redact (e.g., PII masking).

### 6.4 Claim Check

**Book definition**: Store message data in a persistent store and pass a Claim Check to subsequent components.

**Implementation**: `Storage.Cassandra` stores large payloads (exceeding a configurable threshold, default 256 KB). The envelope's payload is replaced with a claim check key (Cassandra row key). Downstream activities retrieve the full payload on demand using the key.

### 6.5 Normalizer

**Book definition**: Route each message type through a custom Message Translator so that the resulting messages match a common format.

**Implementation**: `MessageNormalizer` in `Processing.Transform` routes messages through type-specific transformers based on the `MessageHeaders.ContentType` header. Each transformer converts from the source format (XML, CSV, flat file) to the canonical JSON format used internally.

### 6.6 Canonical Data Model

**Book definition**: Design a Canonical Data Model that is independent of any specific application.

**Implementation**: `IntegrationEnvelope<T>` is the canonical data model. All messages flowing through the platform use this common wrapper regardless of origin or destination. The generic type parameter `T` represents the application-specific payload while the envelope structure is standardized.

---

## 7. Messaging Endpoints

### 7.1 Messaging Gateway

**Book definition**: Use a Messaging Gateway to encapsulate access to the messaging system.

**Implementation**: `Gateway.Api` (`src/Gateway.Api`) provides `IMessagingGateway` and `HttpMessagingGateway`. The gateway exposes a REST API that accepts messages, wraps them in `IntegrationEnvelope`, applies rate limiting, and publishes to the configured broker. Applications interact with the gateway rather than the broker directly.

### 7.2 Messaging Mapper

**Book definition**: Move data between domain objects and the messaging infrastructure.

**Implementation**: `IMessagingMapper` and `JsonMessagingMapper` in `Contracts` (`src/Contracts`) handle serialization and deserialization between domain objects and `IntegrationEnvelope` payloads. The mapper converts application-specific DTOs to and from the canonical envelope format.

### 7.3 Transactional Client

**Book definition**: Make the client's session with the messaging system transactional.

**Implementation**: `ITransactionalClient` and `BrokerTransactionalClient` in `Ingestion` (`src/Ingestion`) provide transactional publish-and-consume semantics. Messages are published and acknowledged atomically — if the processing step fails, the broker acknowledgement is rolled back.

### 7.4 Polling Consumer

**Book definition**: The application explicitly fetches messages when ready.

**Implementation**: `IPollingConsumer` and `PollingConsumer` in `Ingestion` implement pull-based consumption. The consumer polls the broker at configurable intervals, fetches a batch of messages, and processes them. Polling frequency adapts based on queue depth.

### 7.5 Event-Driven Consumer

**Book definition**: The messaging system pushes messages to the consumer.

**Implementation**: `IEventDrivenConsumer` and `EventDrivenConsumer` in `Ingestion` implement push-based consumption. The consumer registers a callback that the broker invokes when new messages arrive. No polling is needed — the broker pushes messages as they appear.

### 7.6 Competing Consumers

**Book definition**: Create multiple consumers on the same channel so they can process messages concurrently.

**Implementation**: `Processing.CompetingConsumers` (`src/Processing.CompetingConsumers`) scales message processing horizontally. Multiple consumer instances listen on the same channel; the broker distributes messages across them. In NATS this uses queue groups, in Kafka consumer groups, in Pulsar Shared subscriptions.

### 7.7 Message Dispatcher

**Book definition**: Create a Message Dispatcher on a channel that consumes a message and distributes it to performers.

**Implementation**: `MessageDispatcher` in `Processing.Dispatcher` (`src/Processing.Dispatcher`) consumes messages from a channel and dispatches them to registered handler functions based on message type. Each handler processes a specific message type independently.

### 7.8 Selective Consumer

**Book definition**: Make the consumer filter which messages it receives.

**Implementation**: `ISelectiveConsumer` and `SelectiveConsumer` in `Ingestion` apply filter predicates at the consumer level. Only messages matching the configured criteria are delivered to the application. Non-matching messages remain on the channel for other consumers.

### 7.9 Durable Subscriber

**Book definition**: The messaging system saves messages published while the subscriber is disconnected.

**Implementation**: `IDurableSubscriber` and `DurableSubscriber` in `Ingestion` maintain persistent subscription state. When the subscriber reconnects, it resumes from the last acknowledged position. All three brokers (Kafka consumer groups, NATS durable consumers, Pulsar durable subscriptions) support this natively.

### 7.10 Idempotent Receiver

**Book definition**: Design a receiver that can handle duplicate messages.

**Implementation**: `Storage.Cassandra` provides a deduplication table. Before processing, the receiver checks if the `MessageId` exists in the dedup table. If found, the message is acknowledged without reprocessing. If not, the MessageId is inserted and processing proceeds.

### 7.11 Service Activator

**Book definition**: Design a Service Activator that connects messages to service operations.

**Implementation**: `ServiceActivator` in `Processing.Dispatcher` (`src/Processing.Dispatcher`) consumes messages from a channel, invokes the appropriate service operation (mapped by message type), wraps the service response in a reply envelope, and publishes it to the reply channel.

---

## 8. System Management

### 8.1 Control Bus

**Book definition**: Use a Control Bus to manage an enterprise integration system.

**Implementation**: `ControlBusPublisher` in `SystemManagement` (`src/SystemManagement`) publishes system management commands (start/stop consumers, update routing rules, enable/disable connectors) to a dedicated control channel. Management components subscribe to the control bus for real-time reconfiguration.

### 8.2 Detour

**Book definition**: Construct a Detour with a context-based router to route messages through testing or debugging steps.

**Implementation**: `Detour` in `Processing.Routing` provides a switchable routing step that can intercept messages and route them through additional processing (e.g., diagnostic logging, message capture, test validation) before continuing to the original destination. The detour is enabled/disabled via the Control Bus.

### 8.3 Wire Tap

**Book definition**: Insert a Wire Tap to inspect messages flowing through a channel.

**Implementation**: `Observability` (`src/Observability`) with OpenTelemetry provides transparent wire tapping. Every processing step emits distributed traces, structured log events, and Prometheus metrics. No explicit tapping configuration is needed — all message processing is observable by default.

### 8.4 Message History

**Book definition**: Attach a Message History to the message that lists all components that have processed it.

**Implementation**: `MessageHistoryHelper` in `Contracts` (`src/Contracts`) appends processing history entries to the envelope's `Metadata` dictionary. Each processing step adds its name, timestamp, and status. The complete history is available for audit and debugging.

### 8.5 Message Store

**Book definition**: Use a Message Store to capture information about each message.

**Implementation**: `MessageStore` in `SystemManagement` (`src/SystemManagement`) provides persistent storage for all messages passing through the platform. Messages are stored with their full envelope, processing history, and delivery status for audit, replay, and compliance requirements.

### 8.6 Smart Proxy

**Book definition**: Use a Smart Proxy to track response messages and correlate them with request messages.

**Implementation**: `SmartProxy` in `SystemManagement` (`src/SystemManagement`) sits between requestors and service providers. It tracks outstanding requests, correlates responses to requests using `CorrelationId`, handles timeouts for unreplied requests, and can redirect replies based on routing rules.

### 8.7 Test Message

**Book definition**: Use Test Message to verify the health of message processing components.

**Implementation**: `TestMessageGenerator` in `SystemManagement` (`src/SystemManagement`) generates synthetic test messages that flow through the processing pipeline. Test messages carry a special header (`MessageHeaders.IsTestMessage`) that prevents side effects on downstream systems. Results are captured for health verification.

### 8.8 Channel Purger

**Book definition**: Use a Channel Purger to remove unwanted messages from a channel.

**Implementation**: `ChannelPurger` in `Ingestion` (`src/Ingestion`) drains all pending messages from a specified channel. This is used during testing, development, and disaster recovery to reset channel state. Purged messages can optionally be logged before removal.

---

## Architecture Notes

### Broker Selection Guide

| Broker | Strength | Best For |
|--------|----------|----------|
| **NATS JetStream** (default) | Lightweight, per-subject independence, no HOL blocking | Local dev, testing, cloud deployments |
| **Kafka** | High-throughput, partitioned, ordered per partition | Broadcast event streams, audit logs, analytics |
| **Pulsar** | Key_Shared subscription, per-key ordering | Large-scale production, recipient-based ordering |

### Message Flow

```
External System → Gateway.Api (rate limiting)
  → Ingestion (broker publish)
    → Workflow.Temporal (orchestration)
      → Processing.* (transform, route, validate)
        → Connector.* (deliver to target)
          → Storage.Cassandra (persist)
            → Observability (trace + metrics)
              → Ack/Nack loopback
```

### Ack/Nack Notification Loopback

Every integration implements atomic notification semantics: all-or-nothing. On success, publish an Ack. On any failure, publish a Nack with compensation details. Downstream systems subscribe to Ack/Nack queues for closed-loop feedback.
