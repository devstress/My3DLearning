# AI Code Generation Strategy

## Overview

The Enterprise Integration Platform leverages Ollama to provide AI-powered code generation capabilities. This document describes how the AI system generates connectors, workflows, and transformation logic, including prompt templates and the validation pipeline that ensures generated code meets platform standards.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     AI Code Generation Pipeline               │
│                                                               │
│  ┌─────────┐    ┌──────────┐    ┌────────┐    ┌───────────┐ │
│  │ User    │    │ Context  │    │ Prompt │    │ Ollama    │ │
│  │ Request │───▶│ Retrieval│───▶│ Builder│───▶│ Inference │ │
│  └─────────┘    └──────────┘    └────────┘    └─────┬─────┘ │
│                                                      │       │
│                                                      ▼       │
│  ┌──────────┐    ┌──────────┐    ┌────────┐    ┌──────────┐ │
│  │ Approved │    │ Human    │    │ Test   │    │ Validate │ │
│  │ Code     │◀───│ Review   │◀───│ Runner │◀───│ & Compile│ │
│  └──────────┘    └──────────┘    └────────┘    └──────────┘ │
└──────────────────────────────────────────────────────────────┘
```

## Connector Generation

### Input Specification

Connectors can be generated from a structured specification:

```json
{
  "connectorName": "AcmeOrderApi",
  "protocol": "HTTP",
  "baseUrl": "https://api.acme.com/v2",
  "authentication": {
    "type": "OAuth2",
    "tokenEndpoint": "https://auth.acme.com/oauth/token",
    "grantType": "client_credentials"
  },
  "operations": [
    {
      "name": "CreateOrder",
      "method": "POST",
      "path": "/orders",
      "requestContentType": "application/json",
      "responseContentType": "application/json",
      "expectedStatus": [201]
    },
    {
      "name": "GetOrderStatus",
      "method": "GET",
      "path": "/orders/{orderId}/status",
      "responseContentType": "application/json",
      "expectedStatus": [200]
    }
  ],
  "retryPolicy": {
    "maxAttempts": 3,
    "backoffMs": 1000
  }
}
```

### Generated Artifacts

From this specification, the AI generates:

1. **Connector class** — Implements `IConnector` with all specified operations
2. **Configuration model** — Strongly-typed settings class
3. **Unit tests** — Tests for each operation, error handling, and retry behavior
4. **Integration test** — Test against a mock HTTP server
5. **Documentation** — Markdown documentation for the connector

### Connector Prompt Template

```
System: You are a senior .NET developer building connectors for an enterprise 
integration platform. Generate production-quality C# code.

Context:
- Target framework: .NET 10
- Connectors implement the IConnector interface
- Use HttpClient with IHttpClientFactory for HTTP operations
- Use Polly for retry and circuit breaker policies
- Include OpenTelemetry ActivitySource for distributed tracing
- Follow existing patterns from the codebase

Reference implementation (retrieved from codebase):
{retrieved_connector_example}

Platform interfaces:
{retrieved_interface_definitions}

User request:
Generate a {protocol} connector with the following specification:
{connector_specification}

Requirements:
1. Implement IConnector interface
2. Create a strongly-typed ConnectorConfiguration class
3. Handle authentication ({auth_type})
4. Implement error handling with proper error classification
5. Add OpenTelemetry tracing spans for each operation
6. Include XML documentation comments
7. Generate xUnit tests with Moq for dependencies

Output format:
- File 1: {ConnectorName}Connector.cs
- File 2: {ConnectorName}Configuration.cs
- File 3: {ConnectorName}ConnectorTests.cs
```

## Workflow Generation

### Input Description

Workflows are generated from business process descriptions:

```yaml
workflowName: OrderProcessing
description: Process incoming orders from partners
steps:
  - name: ValidateOrder
    type: Validate
    schema: OrderSchema.v2
    onFailure: reject
  - name: EnrichCustomerData
    type: Enrich
    source: CRM API
    lookupField: customerId
  - name: TransformToERPFormat
    type: Transform
    sourceSchema: PartnerOrder
    targetSchema: ERPOrder
  - name: DeliverToERP
    type: Deliver
    connector: HttpErpConnector
    compensate: NotifyERPOfCancellation
  - name: SendConfirmation
    type: Notify
    connector: EmailConnector
    template: order-confirmation
saga: true
```

### Workflow Prompt Template

```
System: You are a senior .NET developer building Temporal workflows for an 
enterprise integration platform. Generate production-quality C# code.

Context:
- Target framework: .NET 10
- Use Temporalio .NET SDK
- Workflows inherit from IntegrationWorkflow base class
- Activities implement interface contracts with [Activity] attributes
- Use saga pattern when compensation is needed
- Follow platform retry policies and timeout conventions

Reference workflow (retrieved from codebase):
{retrieved_workflow_example}

Activity interfaces (retrieved from codebase):
{retrieved_activity_interfaces}

User request:
Generate a Temporal workflow for the following business process:
{process_description}

Requirements:
1. Workflow class with [Workflow] attribute
2. Activity interface with [Activity] attributes on each method
3. Activity implementation class
4. Saga compensation if specified
5. Error classification (transient vs permanent)
6. Search attribute updates for visibility
7. OpenTelemetry trace propagation
8. xUnit tests using Temporal test environment

Output format:
- File 1: {WorkflowName}Workflow.cs
- File 2: I{WorkflowName}Activities.cs
- File 3: {WorkflowName}Activities.cs
- File 4: {WorkflowName}WorkflowTests.cs
```

## Transformation Generation

### Input Schema Pair

Transformations are generated from source and target schema examples:

```json
{
  "sourceSchema": {
    "name": "PartnerOrder",
    "example": {
      "orderId": "PO-001",
      "customerName": "Acme Corp",
      "items": [
        { "sku": "WIDGET-A", "qty": 10, "price": 9.99 }
      ],
      "orderDate": "2025-01-15"
    }
  },
  "targetSchema": {
    "name": "ERPOrder",
    "example": {
      "OrderNumber": "PO-001",
      "Customer": { "Name": "Acme Corp" },
      "Lines": [
        { "ItemCode": "WIDGET-A", "Quantity": 10, "UnitPrice": 9.99 }
      ],
      "CreatedDate": "2025-01-15T00:00:00Z"
    }
  },
  "mappingHints": [
    "orderId → OrderNumber",
    "items → Lines (array mapping)",
    "orderDate → CreatedDate (convert to ISO 8601)"
  ]
}
```

### Transformation Prompt Template

```
System: You are generating data transformation logic for an enterprise 
integration platform. Generate correct, efficient C# mapping code.

Context:
- Transformations are Temporal activities
- Input and output are IntegrationEnvelope objects
- Use System.Text.Json for JSON processing
- Handle null values and missing fields gracefully
- Validate output against target schema

Source schema:
{source_schema_definition}

Source example:
{source_example}

Target schema:
{target_schema_definition}

Target example:
{target_example}

Mapping hints:
{mapping_hints}

Requirements:
1. Activity class implementing the transformation
2. Handle all fields from source to target
3. Type conversions where needed
4. Null safety and default values
5. Validation of output
6. xUnit tests with representative test data

Output format:
- File 1: {Source}To{Target}Transform.cs
- File 2: {Source}To{Target}TransformTests.cs
```

## Validation Pipeline

Every AI-generated artifact passes through a multi-stage validation pipeline:

### Stage 1: Syntax Validation

```
Parse generated C# code using Roslyn
  ├── Success → Proceed to Stage 2
  └── Failure → Return to AI with syntax errors for correction (max 3 attempts)
```

### Stage 2: Compilation

```
Compile against platform dependencies
  ├── Success → Proceed to Stage 3
  └── Failure → Return to AI with compiler errors for correction (max 3 attempts)
```

### Stage 3: Static Analysis

```
Run Roslyn analyzers and platform-specific rules
  ├── No violations → Proceed to Stage 4
  └── Violations found → Return to AI with analyzer feedback (max 2 attempts)
```

**Platform Rules Checked:**
- IConnector interface fully implemented
- OpenTelemetry ActivitySource present
- Error handling follows platform patterns
- No hardcoded credentials or connection strings
- Async/await used correctly
- Disposable resources properly managed

### Stage 4: Test Execution

```
Execute generated unit tests
  ├── All pass → Proceed to Stage 5
  └── Failures → Return to AI with test results (max 2 attempts)
```

### Stage 5: Pattern Compliance

```
Verify against platform patterns
  ├── Compliant → Stage 6 (human review)
  └── Non-compliant → Flag deviations for human review
```

**Pattern Checks:**
- Configuration uses the Options pattern
- Dependencies injected via constructor
- Logging follows structured logging conventions
- Metrics use standard naming conventions

### Stage 6: Human Review

All generated code is presented to a developer for review before integration:

- **Diff view** showing generated code
- **Test results** from automated validation
- **Pattern compliance** report
- **Accept / Modify / Reject** workflow

## Quality Metrics

Track AI code generation effectiveness:

| Metric                              | Target   | Description                                 |
|-------------------------------------|----------|---------------------------------------------|
| First-pass compilation rate         | > 85%    | Code compiles without corrections           |
| First-pass test pass rate           | > 70%    | Generated tests pass without corrections    |
| Human acceptance rate               | > 60%    | Code accepted without significant changes   |
| Average correction rounds           | < 2      | Number of AI correction attempts needed     |
| Time savings vs manual              | > 50%    | Developer time saved compared to manual coding|

## Limitations

- **Model size** — Local 7B–13B models produce lower quality than cloud 70B+ models. Accept higher correction rates.
- **Context window** — Limited context means complex multi-file generation may miss cross-file dependencies.
- **Hallucinations** — Generated code may reference non-existent APIs. Compilation catches these.
- **Security** — Generated code must be reviewed for security issues. Static analysis provides automated checks.
- **Testing** — Generated tests may not cover all edge cases. Manual test additions are expected.
