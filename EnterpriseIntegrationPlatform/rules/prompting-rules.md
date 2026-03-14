# Prompting Rules

## Environment Prerequisites

Before implementing any chunk, verify the development environment:

1. **.NET 10 SDK** must be installed. Check with `dotnet --version`.
   - If missing or below `10.0.x`, install from: <https://dotnet.microsoft.com/download/dotnet/10.0>
   - Alternatively use: `winget install Microsoft.DotNet.SDK.10` (Windows), `brew install dotnet-sdk@10` (macOS), or `sudo apt-get install dotnet-sdk-10.0` (Linux)
2. **.NET Aspire templates** must be installed: `dotnet new install Aspire.ProjectTemplates`
3. **Docker** must be running for infrastructure containers (Kafka, NATS, Temporal, Cassandra, Ollama)

See `docs/developer-setup.md` for full setup instructions.

## Resumption Protocol

To continue development, use:

```
continue next chunk
```

The agent must:

1. Read `rules/milestones.md`
2. Identify the next chunk with status `not-started`
3. Implement ONLY that chunk
4. Update chunk status in `milestones.md`
5. Log completion details in `rules/completion-log.md`
6. Ensure the repository remains resumable

## Chunk Implementation Rules

- Implement ONE chunk at a time
- Do not skip ahead to future chunks
- Do not modify completed chunks unless fixing a bug
- Update chunk status to `in-progress` when starting
- Update chunk status to `done` when complete
- Log completion details (files, notes) in `rules/completion-log.md`

## Code Generation Rules

- Generate working, compilable code
- Follow coding standards in `coding-standards.md`
- Follow architecture rules in `architecture-rules.md`
- Include XML documentation on public APIs
- Include unit tests for new logic

## Documentation Rules

- Update relevant docs when architecture changes
- Create ADRs for significant architectural decisions
- Keep `milestones.md` as the source of truth for chunk status (phases and next chunk only)
- Keep `completion-log.md` as the detailed record of completed work

## AI Integration Rules

- Ollama prompts must reference `docs/` and `rules/` context
- Generated code must follow the same standards as hand-written code
- AI-generated files must be clearly attributed
