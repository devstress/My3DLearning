# AI Strategy — Ollama Integration

## Overview

The Enterprise Integration Platform integrates Ollama as a local AI runtime to accelerate development, improve documentation, and assist operations. All AI processing runs on-premises, ensuring that sensitive code and business data never leave the organization's infrastructure.

## Why Local AI

| Concern               | Cloud AI               | Local AI (Ollama)           |
|------------------------|------------------------|-----------------------------|
| Data privacy           | Data sent externally   | Data stays on-premises      |
| Latency                | Network round-trip     | Local inference, low latency|
| Cost                   | Per-token billing      | Fixed infrastructure cost   |
| Availability           | Internet dependency    | Fully offline capable       |
| Customization          | Limited fine-tuning    | Full model control          |

## Ollama Runtime

Ollama runs as a local HTTP service providing LLM inference. The AI provider is configurable — Ollama is the default for on-premises deployment, but the platform supports switching to other AI providers (e.g., Azure OpenAI, AWS Bedrock, or self-hosted alternatives) via the AI service configuration:

- **Endpoint:** `http://localhost:15434` (Aspire host port; container-internal port 11434)
- **Models:** Code-focused models (e.g., CodeLlama, DeepSeek Coder, StarCoder)
- **API:** REST API for generation, chat, and embeddings
- **Resource Requirements:** 8–16 GB RAM for 7B–13B parameter models; GPU acceleration recommended

## Repository Indexing

The AI system indexes the platform's source code and documentation to provide context-aware generation.

### Indexed Sources

| Directory      | Content                          | Purpose                                    |
|----------------|----------------------------------|--------------------------------------------|
| `docs/`        | Architecture, patterns, guides   | Context for documentation generation       |
| `rules/`       | Coding standards, conventions    | Enforce style and patterns in generation   |
| `src/`         | Platform source code             | Learn existing patterns for code generation|

### Indexing Strategy

1. **Chunking** — Source files are split into semantic chunks (functions, classes, sections).
2. **Embedding** — Each chunk is converted to a vector embedding using Ollama's embedding model.
3. **Storage** — Embeddings are stored in RagFlow's internal vector index (Milvus-based) for similarity search.
4. **Retrieval** — When generating code, relevant chunks are retrieved and included as context.

## Self-Hosted GraphRAG

The platform includes a self-hosted RAG (Retrieval-Augmented Generation) system that indexes the repository's own documentation, rules, and source code. This enables any developer on any client machine to generate production-ready integrations without relying on cloud AI services.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Aspire AppHost                            │
│                                                             │
│  ┌──────────┐     ┌──────────────┐     ┌────────────────┐  │
│  │ OpenClaw │────▶│   RagFlow    │────▶│    Ollama      │  │
│  │ Web UI   │     │ (RAG Engine) │     │ (LLM Inference)│  │
│  │ :openclaw│     │ :15080/:15380│     │    :15434      │  │
│  └──────────┘     └──────┬───────┘     └────────────────┘  │
│       │                  │                                  │
│       │           ┌──────┴───────┐                          │
│       │           │  Knowledge   │                          │
│       │           │    Base      │                          │
│       │           │ docs/ rules/ │                          │
│       │           │    src/      │                          │
│       │           └──────────────┘                          │
│       │                                                     │
│       ▼                                                     │
│  POST /api/generate/integration                             │
│  POST /api/generate/chat                                    │
│  GET  /api/generate/datasets                                │
└─────────────────────────────────────────────────────────────┘
```

### How It Works

1. **Index the repo** — Upload `docs/`, `rules/`, and `src/` to RagFlow via its UI (http://localhost:15080). RagFlow chunks, embeds, and stores the content automatically.
2. **Create an assistant** — In RagFlow UI, create a chat assistant linked to the platform datasets. Note the assistant ID.
3. **Configure OpenClaw** — Set `RagFlow:AssistantId` in configuration (user secrets or environment variable).
4. **Generate integrations** — Call `POST /api/generate/integration` with a natural-language description. OpenClaw retrieves relevant context from RagFlow and generates code via Ollama.

### OpenClaw Generation Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/generate/integration` | POST | Generate integration code from a natural-language description. Uses RagFlow for context + Ollama for generation. |
| `/api/generate/chat` | POST | Multi-turn chat with the platform knowledge base. RagFlow handles retrieval + LLM generation. |
| `/api/generate/datasets` | GET | List available RagFlow knowledge base datasets. |
| `/api/health/ragflow` | GET | Check RagFlow service availability. |

### Aspire Port Assignments

All containers use non-common host ports in the 15xxx range to avoid conflicts:

| Service | Host Port | Container Port | Purpose |
|---------|-----------|----------------|---------|
| Ollama | 15434 | 11434 | LLM inference |
| RagFlow UI | 15080 | 80 | Knowledge base management |
| RagFlow API | 15380 | 9380 | RAG retrieval and chat |
| Loki | 15100 | 3100 | Observability log storage |
| Temporal gRPC | 15233 | 7233 | Workflow orchestration |
| Temporal UI | 15280 | 8080 | Workflow inspection |
| NATS | 15222 | 4222 | Message broker |

## Code Generation Capabilities

### Connector Generation

AI generates new connector implementations from high-level specifications:

**Input:** Natural language description or structured specification
```
Generate an HTTP connector for the Acme Order API:
- Base URL: https://api.acme.com/v2
- Authentication: OAuth 2.0 client credentials
- Endpoints: POST /orders, GET /orders/{id}
- Retry: 3 attempts with exponential backoff
- Timeout: 30 seconds
```

**Output:** Complete connector class implementing the `IConnector` interface with:
- Configuration model
- Authentication setup
- Request/response handling
- Error handling and retry logic
- Unit test scaffolding
- OpenTelemetry instrumentation

### Workflow Generation

AI generates Temporal workflow definitions from process descriptions:

**Input:** Business process description
```
Create an order processing workflow:
1. Validate order schema against OrderSchema.json
2. Enrich order with customer data from CRM API
3. Transform order to ERP format
4. Deliver to ERP system via HTTP connector
5. Send confirmation email to customer
6. If ERP delivery fails, compensate by notifying CRM
```

**Output:** Temporal workflow class with:
- Activity definitions for each step
- Saga compensation logic
- Retry policies per activity
- Error handling and DLQ routing
- Integration test scaffolding

### Transformation Logic Generation

AI generates transformation mappings between source and target schemas:

**Input:** Source schema, target schema, and optional mapping hints
```
Source: OrderEvent (JSON)
  - orderId, customerName, items[{sku, qty, price}], orderDate
Target: ERPOrder (XML)
  - OrderNumber, Customer/Name, Lines/Line[{ItemCode, Quantity, UnitPrice}], CreatedDate
```

**Output:** Transformation activity with:
- Field mapping logic
- Type conversions
- Nested structure handling
- Default values and null handling
- Validation of mapped output

### Milestone Summarization

AI generates summaries of development progress for stakeholder communication:

**Input:** Git log, PR descriptions, and issue tracker data
**Output:** Structured milestone summary including:
- Features completed
- Technical debt addressed
- Known issues and risks
- Next milestone objectives

## Prompt Templates

### Connector Generation Prompt

```
You are a .NET developer building connectors for the Enterprise Integration Platform.

Context:
- Connectors implement the IConnector interface
- Use HttpClient with Polly for HTTP resilience
- Include OpenTelemetry ActivitySource for tracing
- Follow the existing connector pattern in src/Connectors/

Existing connector example:
{retrieved_context}

Generate a connector for:
{user_specification}

Include:
1. Connector class
2. Configuration model
3. Unit tests
4. README documentation
```

### Workflow Generation Prompt

```
You are generating a Temporal workflow for the Enterprise Integration Platform.

Context:
- Workflows inherit from IntegrationWorkflow base class
- Activities are defined as interfaces with [Activity] attributes
- Use saga pattern for multi-step processes requiring compensation
- Follow retry policies defined in platform configuration

Existing workflow example:
{retrieved_context}

Generate a workflow for:
{process_description}
```

## Validation Pipeline

All AI-generated code passes through a validation pipeline before use:

```
AI Generation → Syntax Check → Compilation → Static Analysis → Unit Tests → Human Review
```

### Validation Steps

1. **Syntax Check** — Parse the generated code to verify valid C# syntax.
2. **Compilation** — Compile the generated code against platform dependencies.
3. **Static Analysis** — Run Roslyn analyzers and platform-specific rules.
4. **Unit Tests** — Execute generated tests and verify they pass.
5. **Pattern Compliance** — Verify adherence to platform patterns (interface implementation, telemetry, error handling).
6. **Human Review** — Developer reviews, modifies if needed, and approves for integration.

## Integration Architecture

```
┌─────────────────────────────────────────────────┐
│                  AI Service                      │
│                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────────┐  │
│  │ Prompt   │  │ Context  │  │ Validation   │  │
│  │ Builder  │──│ Retriever│──│ Pipeline     │  │
│  └──────────┘  └──────────┘  └──────────────┘  │
│       │              │               │           │
│       ▼              ▼               ▼           │
│  ┌──────────────────────────────────────────┐   │
│  │           Ollama HTTP Client             │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────┘
                      │
                      ▼
              ┌──────────────┐
              │ Ollama Server│
              │ (localhost)  │
              └──────────────┘
```

## Limitations and Guardrails

- **No production execution** — AI-generated code is never executed in production without human review.
- **No secret handling** — AI prompts never include credentials, connection strings, or sensitive data.
- **Model limitations** — Local models may produce lower quality output than large cloud models; validation pipeline catches errors.
- **Hallucination risk** — Generated code may reference non-existent APIs; compilation and testing catch these issues.
- **Context window** — Limited context window size requires careful chunking and retrieval strategies.
