# Developer Setup Guide

## Prerequisites

| Tool | Required Version | Installation |
|------|-----------------|--------------|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Git | Latest | [Download](https://git-scm.com/) |

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/devstress/My3DLearning.git
cd My3DLearning/Terranes
```

### 2. Verify .NET SDK

```bash
dotnet --version
# Expected: 10.0.x
```

### 3. Restore and Build

```bash
dotnet restore
dotnet build
```

### 4. Run Tests

```bash
dotnet test
```

## Project Structure

The solution follows a modular architecture with clear separation of concerns:

- **`src/Contracts/`** — Core DTOs, interfaces, and canonical models shared across all projects
- **`tests/UnitTests/`** — Fast, isolated unit tests using NUnit

## Development Rules

Before contributing, read:

- [Coding Standards](../rules/coding-standards.md) — Language, naming, style, testing conventions
- [Architecture Rules](../rules/architecture-rules.md) — Dependency rules, communication patterns, data rules
- [Quality Pillars](../rules/quality-pillars.md) — The 11 architectural quality attributes every change must satisfy
- [Reality Filter](../rules/reality-filter.md) — AI agent enforcement rules (no pretend code, no stubs)
- [Milestones](../rules/milestones.md) — Current development status and next chunk

## Chunk-Based Development

Development follows a chunk-based incremental delivery model. Each chunk is self-contained, testable, and resumable.

To continue development:

```
continue next chunk
```

See [Prompting Rules](../rules/prompting-rules.md) for the full resumption protocol.
